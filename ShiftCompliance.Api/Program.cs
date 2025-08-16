using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ShiftCompliance.Api.Data;
using ShiftCompliance.Api.Models;
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
        Description = "Shift image uploads + compliance analysis"
    });
});

// ── EF Core: SQL Server ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,   // <- ensure key "Default" exists
        sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            sql.CommandTimeout(60);
        });
});

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IStorageService, LocalStorageService>();

// Pick ONE analyzer. Use BrightnessAnalyzer for now; swap to AzureCustomVisionAnalyzer later.
// builder.Services.AddHttpClient<IImageAnalyzer, AzureCustomVisionAnalyzer>();
builder.Services.AddSingleton<IImageAnalyzer, BrightnessAnalyzer>();

var app = builder.Build();

// ── Swagger UI ─────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShiftCompliance.Api v1");
    c.RoutePrefix = "swagger"; // UI at /swagger
});

// ── Endpoints ──────────────────────────────────────────────────────────────────

// Upload an image, analyze, and store a ShiftLog
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

    // (Optional) basic validation
    var allowed = new[] { ".jpg", ".jpeg", ".png" };
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowed.Contains(ext)) return Results.BadRequest("Only JPG/PNG allowed.");
    if (file.Length > 10 * 1024 * 1024) return Results.BadRequest("Max 10MB.");

    await using var s = file.OpenReadStream();
    var savedPath = await storage.SaveAsync(s, file.FileName, ct);

    var analysis = await analyzer.AnalyzeAsync(savedPath, ct);

    var timestampUtc = DateTime.UtcNow;
    if (DateTime.TryParse(tsLocal, out var providedLocal))
        timestampUtc = providedLocal.ToUniversalTime();

    var log = new ShiftLog
    {
        Operator = op,
        Shift = shift,
        TimestampUtc = timestampUtc,
        ImagePath = savedPath,
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
        score = Math.Round(log.Score, 4)
    });
})
.WithSummary("Upload a shift-end photo and analyze compliance");

// Daily summary (overall, by shift, by operator)
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

// Rolling multi-day trend
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

    return Results.Ok(new
    {
        start = DateOnly.FromDateTime(start),
        end = DateOnly.FromDateTime(end),
        byDay
    });
})
.WithSummary("Rolling multi-day trend (default 7 days)");

app.Run();
