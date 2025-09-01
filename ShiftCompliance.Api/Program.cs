using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShiftCompliance.Api.Data;
using ShiftCompliance.Api.Models;
using ShiftCompliance.Api.Models.Dtos;
using ShiftCompliance.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger / OpenAPI ───────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ShiftCompliance.Api",
        Version = "v1",
        Description = "Shift image uploads + compliance analysis + production entries"
    });
});

// ── EF Core: SQL Server ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 5,
                                     maxRetryDelay: TimeSpan.FromSeconds(10),
                                     errorNumbersToAdd: null);
            sql.CommandTimeout(60);
        });
});

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddSingleton<IImageAnalyzer, MarkerColorAnalyzer>();
builder.Services.AddScoped<INumberSeries, SimpleNumberSeries>();
builder.Services.AddCors(o =>
{
    o.AddPolicy("web", p =>
        p.WithOrigins(
             "https://localhost:7098",
             "http://localhost:7098")   // your MVC app
         .AllowAnyHeader()
         .AllowAnyMethod());
});


// Avoid JSON reference loops anywhere we serialize
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// ── Seed (demo) ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Supervisors.Any())
    {
        db.Supervisors.AddRange(
            new Supervisor { Name = "ADMIN" },
            new Supervisor { Name = "Ayo" },
            new Supervisor { Name = "Mr. Conley" },
            new Supervisor { Name = "Mr. Tunde" }
        );
    }

    if (!db.Items.Any())
    {
        db.Items.AddRange(
            new Item { ItemNo = "1896-S", Description = "Sprocket", UnitOfMeasure = "PCS" },
            new Item { ItemNo = "AX-100", Description = "Axle", UnitOfMeasure = "PCS" }
        );
    }

    db.SaveChanges();
}
app.UseCors("web");
// ── Swagger UI ─────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShiftCompliance.Api v1");
    c.RoutePrefix = "swagger"; // UI at /swagger
});

// ── Static files (serve /uploads/... from wwwroot) ─────────────────────────────
app.UseStaticFiles();

// ── Shift Endpoints ────────────────────────────────────────────────────────────
app.MapPost("/api/shifts/upload", async (
    AppDbContext db,
    IStorageService storage,
    IImageAnalyzer analyzer,
    HttpRequest request,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType) return Results.BadRequest("multipart/form-data expected.");

    var form = await request.ReadFormAsync(ct);
    var file = form.Files["Image"];
    var op = form["Operator"].ToString();
    var shift = form["Shift"].ToString();
    var tsLocal = form["TimestampLocal"].ToString();

    if (file is null || file.Length == 0) return Results.BadRequest("Image required.");
    if (string.IsNullOrWhiteSpace(op)) return Results.BadRequest("Operator required.");
    if (string.IsNullOrWhiteSpace(shift)) return Results.BadRequest("Shift required.");

    var allowed = new[] { ".jpg", ".jpeg", ".png" };
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowed.Contains(ext)) return Results.BadRequest("Only JPG/PNG allowed.");
    if (file.Length > 10 * 1024 * 1024) return Results.BadRequest("Max 10MB.");

    await using var s = file.OpenReadStream();
    var webPath = await storage.SaveAsync(s, file.FileName, ct);       // web path for UI
    var localPath = storage.MapWebPathToPhysical(webPath);             // physical for analyzer

    var analysis = await analyzer.AnalyzeAsync(localPath, ct);

    var timestampUtc = DateTime.UtcNow;
    if (DateTime.TryParse(tsLocal, out var providedLocal))
        timestampUtc = providedLocal.ToUniversalTime();

    var log = new ShiftLog
    {
        Operator = op,
        Shift = shift,
        TimestampUtc = timestampUtc,
        ImagePath = webPath,
        IsCompliant = analysis.IsCompliant,
        Score = analysis.Score
    };

    db.ShiftLogs.Add(log);
    await db.SaveChangesAsync(ct);

    return Results.Ok(new
    {
        id = log.Id,
        log.Operator,
        log.Shift,
        timestampUtc = log.TimestampUtc,
        compliance = log.IsCompliant,
        score = Math.Round(log.Score, 4),
        imageUrl = log.ImagePath
    });
})
.DisableAntiforgery()
.WithSummary("Upload a shift-end photo and analyze compliance");

app.MapGet("/api/shifts/daily-summary", async (AppDbContext db, DateOnly? day, CancellationToken ct) =>
{
    var target = day ?? DateOnly.FromDateTime(DateTime.UtcNow);
    var start = target.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var end = target.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

    var items = await db.ShiftLogs
        .Where(x => x.TimestampUtc >= start && x.TimestampUtc <= end)
        .ToListAsync(ct);

    return Results.Ok(new
    {
        date = target,
        total = items.Count,
        compliant = items.Count(x => x.IsCompliant),
        nonCompliant = items.Count(x => !x.IsCompliant),
        byShift = items.GroupBy(x => x.Shift)
                       .ToDictionary(g => g.Key, g => new
                       {
                           total = g.Count(),
                           compliant = g.Count(x => x.IsCompliant),
                           nonCompliant = g.Count(x => !x.IsCompliant)
                       }),
        byOperator = items.GroupBy(x => x.Operator)
                          .ToDictionary(g => g.Key, g => new
                          {
                              total = g.Count(),
                              compliant = g.Count(x => x.IsCompliant),
                              nonCompliant = g.Count(x => !x.IsCompliant)
                          })
    });
})
.WithSummary("Daily compliance summary (overall, by shift, by operator)");

app.MapGet("/api/shifts/rolling-summary", async (AppDbContext db, int days, CancellationToken ct) =>
{
    if (days <= 0 || days > 90) days = 7;
    var end = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
    var start = DateTime.UtcNow.Date.AddDays(-days + 1);

    var items = await db.ShiftLogs
        .Where(x => x.TimestampUtc >= start && x.TimestampUtc <= end)
        .ToListAsync(ct);

    var byDay = items.GroupBy(x => DateOnly.FromDateTime(x.TimestampUtc))
        .OrderBy(g => g.Key)
        .ToDictionary(g => g.Key, g => new
        {
            total = g.Count(),
            compliant = g.Count(x => x.IsCompliant),
            nonCompliant = g.Count(x => !x.IsCompliant)
        });

    return Results.Ok(new { start = DateOnly.FromDateTime(start), end = DateOnly.FromDateTime(end), byDay });
})
.WithSummary("Rolling multi-day trend (default 7 days)");

// ── Item Endpoints ─────────────────────────────────────────────────────────────
app.MapGet("/api/items/{itemNo}", async (string itemNo, AppDbContext db, CancellationToken ct) =>
{
    var item = await db.Items.FirstOrDefaultAsync(i => i.ItemNo == itemNo && i.IsActive, ct);
    return item is null ? Results.NotFound() : Results.Ok(item);
})
.WithSummary("Get one item by ItemNo");

app.MapGet("/api/items", async (string? q, int top, AppDbContext db, CancellationToken ct) =>
{
    if (top <= 0 || top > 50) top = 20;
    q ??= "";
    var list = await db.Items
        .Where(i => i.IsActive && (i.ItemNo.Contains(q) || i.Description.Contains(q)))
        .OrderBy(i => i.ItemNo)
        .Take(top)
        .Select(i => new { i.ItemNo, i.Description, i.UnitOfMeasure })
        .ToListAsync(ct);
    return Results.Ok(list);
})
.WithSummary("Search items for autocomplete");

// ── Production Endpoints ───────────────────────────────────────────────────────
app.MapPost("/api/production", async (
    [FromForm] ProductionCreateDto dto,
    HttpContext http,
    AppDbContext db,
    IStorageService storage,
    IImageAnalyzer analyzer,
    INumberSeries nos,
    CancellationToken ct) =>
{
    // Accept lines via multipart as LinesJson
    if ((dto.Lines == null || dto.Lines.Count == 0) &&
        http.Request.HasFormContentType &&
        http.Request.Form.TryGetValue("LinesJson", out var linesJson))
    {
        var parsed = JsonSerializer.Deserialize<List<ProductionLineDto>>(linesJson!);
        if (parsed != null) dto.Lines = parsed;
    }

    if (dto.Lines == null || dto.Lines.Count == 0)
        return Results.BadRequest("At least one line is required.");

    // Validate items
    var itemNos = dto.Lines.Select(l => l.ItemNo).Distinct().ToList();
    var items = await db.Items.Where(i => itemNos.Contains(i.ItemNo) && i.IsActive).ToListAsync(ct);
    if (items.Count != itemNos.Count)
        return Results.BadRequest("One or more ItemNo values do not exist or are inactive.");
    var itemMap = items.ToDictionary(i => i.ItemNo, i => i);

    var number = string.IsNullOrWhiteSpace(dto.No) ? await nos.NextAsync() : dto.No;

    // Resolve supervisor name (Id preferred; fallback to free-text)
    string supervisorName = dto.ShiftSupervisor ?? "";
    if (dto.ShiftSupervisorId is int supId)
    {
        var sup = await db.Supervisors.FirstOrDefaultAsync(s => s.Id == supId && s.IsActive, ct);
        if (sup != null) supervisorName = sup.Name;
    }

    string? webPath = null;
    bool? isCompliant = null;
    float? score = null;

    if (dto.Image is { Length: > 0 })
    {
        var ext = Path.GetExtension(dto.Image.FileName);
        var desiredFileName = $"{number}{ext}"; // e.g., "P-0003.jpg"

        await using var s = dto.Image.OpenReadStream();
        webPath = await storage.SaveAsAsync(s, desiredFileName, ct);

        var localPath = storage.MapWebPathToPhysical(webPath);
        var res = await analyzer.AnalyzeAsync(localPath, ct);
        isCompliant = res.IsCompliant;
        score = res.Score;
    }

    var postingUtc = dto.PostingDateLocal?.ToUniversalTime();

    var entry = new ProductionEntry
    {
        No = number,
        Description = dto.Description ?? "",
        Shift = dto.Shift,
        ShiftSupervisor = supervisorName,
        PostingDateUtc = postingUtc,
        Remark = dto.Remark ?? "",
        ImagePath = webPath,
        IsCompliant = isCompliant,
        ComplianceScore = score,
        Lines = dto.Lines.Select(l => new ProductionEntryLine
        {
            LineNo = l.LineNo,
            ItemNo = l.ItemNo,
            Quantity = l.Quantity,
            UnitOfMeasure = string.IsNullOrWhiteSpace(l.UnitOfMeasure)
                                ? itemMap[l.ItemNo].UnitOfMeasure
                                : l.UnitOfMeasure,
            DowntimeMinutes = l.DowntimeMinutes,
            OvertimeHours = l.OvertimeHours,
            SafetyIncidents = l.SafetyIncidents,
            Remark = l.Remark ?? ""
        }).ToList()
    };

    db.ProductionEntries.Add(entry);
    await db.SaveChangesAsync(ct);

    return Results.Ok(new
    {
        id = entry.Id,
        entry.No,
        entry.Description,
        entry.Shift,
        entry.ShiftSupervisor,
        entry.PostingDateUtc,
        entry.IsCompliant,
        score = entry.ComplianceScore,
        imageUrl = entry.ImagePath,
        lines = entry.Lines.Count
    });
})
.DisableAntiforgery()
.Accepts<ProductionCreateDto>("multipart/form-data")
.WithSummary("Create a production entry (header + lines) with optional image analysis");

// Paged + Filtered list: /api/production?page=1&pageSize=20&dateFrom=2025-08-01&dateTo=2025-08-31&shift=Morning&supervisor=Conley&compliant=true
app.MapGet(
    "/api/production",
    async (
        int page,
        int pageSize,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? shift,
        string? supervisor,
        bool? compliant,
        AppDbContext db,
        CancellationToken ct) =>
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;
        if (pageSize > 100) pageSize = 100;

        var q = db.ProductionEntries.AsQueryable();

        // Date filter (assumes PostingDateUtc is UTC)
        if (dateFrom.HasValue)
        {
            var start = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(x => x.PostingDateUtc >= start);
        }
        if (dateTo.HasValue)
        {
            var end = dateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(x => x.PostingDateUtc <= end);
        }

        if (!string.IsNullOrWhiteSpace(shift))
            q = q.Where(x => x.Shift == shift);

        if (!string.IsNullOrWhiteSpace(supervisor))
            q = q.Where(x => x.ShiftSupervisor.Contains(supervisor));

        if (compliant.HasValue)
            q = q.Where(x => x.IsCompliant == compliant.Value);

        q = q.OrderByDescending(x => x.PostingDateUtc);

        var total = await q.CountAsync(ct);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.No,
                x.Description,
                x.Shift,
                x.ShiftSupervisor,
                x.PostingDateUtc,
                x.IsCompliant,
                x.ComplianceScore
            })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items
        });
    })
.WithSummary("Paged & filtered list of production entries");


// Details (cycle-free projection)
app.MapGet("/api/production/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var entry = await db.ProductionEntries
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, ct);

    if (entry is null) return Results.NotFound();

    var payload = new
    {
        entry.Id,
        entry.No,
        entry.Description,
        entry.Shift,
        entry.ShiftSupervisor,
        entry.PostingDateUtc,
        entry.IsCompliant,
        ComplianceScore = entry.ComplianceScore,
        ImagePath = entry.ImagePath,
        Lines = entry.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new
            {
                l.LineNo,
                l.ItemNo,
                l.Quantity,
                l.UnitOfMeasure,
                l.DowntimeMinutes,
                l.OvertimeHours,
                l.SafetyIncidents,
                l.Remark
            })
            .ToList()
    };

    return Results.Ok(payload);
})
.WithSummary("Get a production entry by id (includes lines)");

// Supervisors lookup
app.MapGet("/api/supervisors", async (string? q, int top, AppDbContext db, CancellationToken ct) =>
{
    if (top <= 0 || top > 50) top = 20;
    q ??= "";
    var list = await db.Supervisors
        .Where(s => s.IsActive && s.Name.Contains(q))
        .OrderBy(s => s.Name)
        .Take(top)
        .Select(s => new { s.Id, s.Name })
        .ToListAsync(ct);

    return Results.Ok(list);
})
.WithSummary("Active supervisors for dropdown");


// ---- Shift-level budgets (whole shift/day) ----

// Create or upsert one
app.MapPost("/api/budgets/shift", async (ShiftBudget dto, AppDbContext db, CancellationToken ct) =>
{
    // Normalize: only one active row per Date+Shift
    var existing = await db.ShiftBudgets
        .FirstOrDefaultAsync(x => x.Date == dto.Date && x.Shift == dto.Shift, ct);

    if (existing is null)
        db.ShiftBudgets.Add(dto);
    else
    {
        existing.TargetQty = dto.TargetQty;
        existing.Remark = dto.Remark;
        existing.IsActive = dto.IsActive;
    }

    await db.SaveChangesAsync(ct);
    return Results.Ok(dto);
});

// Get (optionally by range)
app.MapGet("/api/budgets/shift", async (DateOnly? from, DateOnly? to, AppDbContext db, CancellationToken ct) =>
{
    var q = db.ShiftBudgets.AsQueryable();
    if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
    if (to.HasValue) q = q.Where(x => x.Date <= to.Value);

    var rows = await q.OrderBy(x => x.Date).ThenBy(x => x.Shift).ToListAsync(ct);
    return Results.Ok(rows);
});

// Delete
app.MapDelete("/api/budgets/shift/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var b = await db.ShiftBudgets.FindAsync(new object?[] { id }, ct);
    if (b is null) return Results.NotFound();
    db.Remove(b);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// GET /api/reports/shift-budget-vs-actual?dateFrom=2025-08-01&dateTo=2025-08-31&shift=Morning&itemNo=AX-100


app.MapGet("/api/reports/shift-budget-vs-actual", async (
    DateOnly? dateFrom,
    DateOnly? dateTo,
    string? shift,
    string? itemNo,
    AppDbContext db,
    CancellationToken ct) =>
{
    // Avoid variable names 'from'/'to' near query syntax
    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    var winStart = dateFrom ?? today.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
    var winEnd   = dateTo   ?? winStart.AddDays(6);
    if (winEnd < winStart) (winStart, winEnd) = (winEnd, winStart);

    // 1) ACTIVE budgets only (status-driven)
    var budgetPerShift = await db.ShiftItemBudgets.AsNoTracking()
        .Where(b => b.IsActive
            && (string.IsNullOrWhiteSpace(shift)  || b.Shift == shift)
            && (string.IsNullOrWhiteSpace(itemNo) || b.ItemNo == itemNo))
        .GroupBy(b => b.Shift)
        .Select(g => new { g.Key, Budget = g.Sum(x => x.TargetQty) })
        .ToDictionaryAsync(x => x.Key, x => x.Budget, StringComparer.Ordinal, ct);

    // 2) Actuals grouped by Date+Shift within the window
    var actuals = await db.ProductionEntryLines.AsNoTracking()
        .Where(l => l.ProductionEntry!.PostingDateUtc != null)
        .Where(l =>
            DateOnly.FromDateTime(l.ProductionEntry!.PostingDateUtc!.Value) >= winStart &&
            DateOnly.FromDateTime(l.ProductionEntry!.PostingDateUtc!.Value) <= winEnd &&
            (string.IsNullOrWhiteSpace(shift)  || l.ProductionEntry!.Shift == shift) &&
            (string.IsNullOrWhiteSpace(itemNo) || l.ItemNo == itemNo))
        .GroupBy(l => new
        {
            Date  = DateOnly.FromDateTime(l.ProductionEntry!.PostingDateUtc!.Value),
            Shift = l.ProductionEntry!.Shift
        })
        .Select(g => new { g.Key.Date, g.Key.Shift, Actual = g.Sum(x => x.Quantity) })
        .ToListAsync(ct);

    var actualMap = actuals.ToDictionary(a => (a.Date, a.Shift), a => a.Actual);

    // 3) Build rows for each day; budget is constant per shift (current active)
    var days = Enumerable.Range(0, (int)(winEnd.ToDateTime(TimeOnly.MinValue) - winStart.ToDateTime(TimeOnly.MinValue)).TotalDays + 1)
                         .Select(i => winStart.AddDays(i))
                         .ToArray();

    static string GradeFromPct(decimal? pct)
        => !pct.HasValue ? "N/A" : (pct.Value >= 100 ? "Green" : pct.Value >= 95 ? "Yellow" : "Red");

    string[] shiftOrder = new[] { "Morning", "Afternoon", "Night" };

    // shifts to show = shifts with an active budget OR with any actuals
    var shiftsToShow = new HashSet<string>(budgetPerShift.Keys, StringComparer.Ordinal);
    foreach (var s in actuals.Select(a => a.Shift)) shiftsToShow.Add(s);

    var rows = (from d in days
                from sh in shiftsToShow
                let budget   = budgetPerShift.TryGetValue(sh, out var b) ? b : 0m
                let actual   = actualMap.TryGetValue((d, sh), out var a) ? a : 0m
                let variance = actual - budget
                let pct      = budget == 0 ? (actual == 0 ? (decimal?)null : 100m)
                                           : Math.Round(actual / budget * 100m, 2)
                select new
                {
                    date = d,
                    shift = sh,
                    budget,
                    actual,
                    variance,
                    attainmentPct = pct,
                    grade = GradeFromPct(pct)
                })
               .OrderBy(r => r.date)
               .ThenBy(r => { var i = Array.IndexOf(shiftOrder, r.shift); return i < 0 ? 99 : i; })
               .ToList();

    // Totals
    var totalBudget   = rows.Sum(r => r.budget);
    var totalActual   = rows.Sum(r => r.actual);
    var totalVariance = totalActual - totalBudget;
    decimal? totalPct = totalBudget == 0 ? (totalActual == 0 ? (decimal?)null : 100m)
                                         : Math.Round(totalActual / totalBudget * 100m, 2);

    return Results.Ok(new
    {
        filters = new { dateFrom = winStart, dateTo = winEnd, shift, itemNo },
        totals = new
        {
            budget = totalBudget,
            actual = totalActual,
            variance = totalVariance,
            attainmentPct = totalPct,
            grade = GradeFromPct(totalPct)
        },
        rows
    });
})
.WithSummary("Budget vs Actual (status-driven: uses only current active budgets)");


// POST /api/budgets/upsert  (create or update one item-level budget)
// POST /api/budgets/upsert  (history-friendly version)
// POST /api/budgets/upsert  (status-driven; keep history; enforce one active)
app.MapPost("/api/budgets/upsert", async (
    [FromBody] BudgetUpsertDto dto,
    AppDbContext db,
    CancellationToken ct) =>
{
    // --- validation ---
    if (string.IsNullOrWhiteSpace(dto.Shift))
        return Results.BadRequest("Shift is required.");
    if (string.IsNullOrWhiteSpace(dto.ItemNo))
        return Results.BadRequest("ItemNo is required.");
    if (dto.TargetQty < 0)
        return Results.BadRequest("TargetQty cannot be negative.");

    var shift = dto.Shift.Trim();
    var item = dto.ItemNo.Trim();

    // >>> IMPORTANT: use the execution strategy when you use an explicit transaction
    var strategy = db.Database.CreateExecutionStrategy();

    try
    {
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            // Deactivate any currently-active rows for this (Shift, Item)
            var previouslyActive = await db.ShiftItemBudgets
                .Where(b => b.Shift == shift && b.ItemNo == item && b.IsActive)
                .ToListAsync(ct);

            foreach (var p in previouslyActive)
            {
                p.IsActive = false;
                p.UpdatedUtc = DateTime.UtcNow;
                db.ShiftItemBudgets.Update(p);
            }

            // Insert new ACTIVE row (history kept)
            var row = new ShiftItemBudget
            {
                Shift = shift,
                ItemNo = item,
                Date = dto.Date,            // audit only
                TargetQty = dto.TargetQty,
                Remark = dto.Remark,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            db.ShiftItemBudgets.Add(row);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Results.Ok(new { ok = true, id = row.Id });
        });
    }
    catch (DbUpdateException ex)
    {
        return Results.Problem(
            title: "Conflict saving budget (DB)",
            detail: ex.InnerException?.Message ?? ex.Message,
            statusCode: StatusCodes.Status409Conflict);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Unexpected error saving budget",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
});


// DELETE /api/budgets/items/{id}
app.MapDelete("/api/budgets/items/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var row = await db.ShiftItemBudgets.FindAsync(new object?[] { id }, ct);
    if (row is null) return Results.NotFound();
    db.ShiftItemBudgets.Remove(row);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.WithSummary("Delete a ShiftItemBudget by Id");

// GET /api/budgets/items/active?shift=...&itemNo=...
app.MapGet("/api/budgets/items/active", async (
    string? shift,
    string? itemNo,
    AppDbContext db,
    CancellationToken ct) =>
{
    var list = await db.ShiftItemBudgets.AsNoTracking()
        .Where(b => b.IsActive
                 && (string.IsNullOrWhiteSpace(shift) || b.Shift == shift)
                 && (string.IsNullOrWhiteSpace(itemNo) || b.ItemNo == itemNo))
        .OrderBy(b => b.Shift).ThenBy(b => b.ItemNo)
        .Select(b => new { b.Id, b.Shift, b.ItemNo, b.TargetQty, b.Remark })
        .ToListAsync(ct);

    return Results.Ok(list);
})
.WithSummary("List only the active budgets per (Shift, ItemNo)");


// GET /api/budgets/items?from=2025-08-01&to=2025-08-31&shift=Morning&itemNo=1896-S&page=1&pageSize=20&onlyActive=true
app.MapGet("/api/budgets/items", async (
    DateOnly? from,
    DateOnly? to,
    string? shift,
    string? itemNo,
    bool? onlyActive,                 // if true => status-driven (active only), ignore from/to
    int page,
    int pageSize,
    AppDbContext db,
    CancellationToken ct) =>
{
    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 20 : (pageSize > 200 ? 200 : pageSize);

    var q = db.ShiftItemBudgets.AsNoTracking().AsQueryable();

    // Status-driven: show only the single active budget per (Shift, ItemNo)
    if (onlyActive == true)
    {
        q = q.Where(b => b.IsActive);
    }
    else
    {
        // Historical browsing: optional filter by the informational Date field
        if (from.HasValue) q = q.Where(b => b.Date >= from.Value);
        if (to.HasValue) q = q.Where(b => b.Date <= to.Value);
    }

    if (!string.IsNullOrWhiteSpace(shift)) q = q.Where(b => b.Shift == shift);
    if (!string.IsNullOrWhiteSpace(itemNo)) q = q.Where(b => b.ItemNo == itemNo);

    // Order: for active view, by Shift/Item; for history, by Date desc then Shift/Item
    if (onlyActive == true)
        q = q.OrderBy(b => b.Shift).ThenBy(b => b.ItemNo);
    else
        q = q.OrderByDescending(b => b.Date).ThenBy(b => b.Shift).ThenBy(b => b.ItemNo);

    var total = await q.CountAsync(ct);

    var items = await q.Skip((page - 1) * pageSize)
                       .Take(pageSize)
                       .Select(b => new
                       {
                           b.Id,
                           b.Date,
                           b.Shift,
                           b.ItemNo,
                           b.TargetQty,
                           b.Remark,
                           b.IsActive
                       })
                       .ToListAsync(ct);

    return Results.Ok(new
    {
        page,
        pageSize,
        total,
        totalPages = (int)Math.Ceiling(total / (double)pageSize),
        items
    });
})
.WithSummary("List item-level shift budgets (paged). Set onlyActive=true for status-driven view.");


app.Run();
