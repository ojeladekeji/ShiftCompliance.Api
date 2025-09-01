using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using ShiftCompliance.Web.Models.Vm;
using System.Globalization;
using System.Text;

namespace ShiftCompliance.Web.Controllers
{
    public class ReportsController(IHttpClientFactory http, ILogger<ReportsController> logger) : Controller
    {
        // GET: /Reports/ShiftBudgetVsActual
        // Uses status-driven API (active budgets), with optional date, shift, itemNo filters
        [HttpGet]
        public async Task<IActionResult> ShiftBudgetVsActual([FromQuery] ShiftBudgetQueryVm query)
        {
            var today = DateTime.UtcNow.Date;
            var start = query.DateFrom?.ToDateTime(TimeOnly.MinValue)
                        ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var end = query.DateTo?.ToDateTime(TimeOnly.MinValue) ?? start.AddDays(6);

            var q = new ShiftBudgetQueryVm
            {
                DateFrom = DateOnly.FromDateTime(start),
                DateTo = DateOnly.FromDateTime(end),
                Shift = string.IsNullOrWhiteSpace(query.Shift) ? null : query.Shift,
                ItemNo = string.IsNullOrWhiteSpace(query.ItemNo) ? null : query.ItemNo
            };

            var client = http.CreateClient("ShiftApi");

            var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);
            qs["dateFrom"] = q.DateFrom?.ToString("yyyy-MM-dd");
            qs["dateTo"] = q.DateTo?.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(q.Shift)) qs["shift"] = q.Shift;
            if (!string.IsNullOrWhiteSpace(q.ItemNo)) qs["itemNo"] = q.ItemNo;

            var url = $"api/reports/shift-budget-vs-actual?{qs}";
            HttpResponseMessage res;
            try
            {
                res = await client.GetAsync(url);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to call report endpoint {Url}", url);
                ViewBag.Error = $"Failed to run report ({ex.Message}).";
                return View(new ShiftBudgetReportPageVm { Query = q, Totals = new(), Rows = new(), Items = await LoadItemsAsync(client, logger) });
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            // Build rows from API
            var rows = new List<ShiftBudgetRowVm>();
            foreach (var r in root.GetProperty("rows").EnumerateArray())
            {
                var row = new ShiftBudgetRowVm
                {
                    Date = DateOnly.Parse(r.GetProperty("date").GetString()!),
                    Shift = r.GetProperty("shift").GetString()!,
                    Budget = r.GetProperty("budget").GetDecimal(),
                    Actual = r.GetProperty("actual").GetDecimal(),
                    Variance = r.GetProperty("variance").GetDecimal(),
                    AttainmentPct = r.GetProperty("attainmentPct").ValueKind == JsonValueKind.Null
                                        ? null : r.GetProperty("attainmentPct").GetDecimal(),
                    Grade = r.GetProperty("grade").GetString() ?? "N/A"
                };
                row.GradeColorClass = row.Grade switch
                {
                    "Green" => "bg-success text-white",
                    "Yellow" => "bg-warning",
                    "Red" => "bg-danger text-white",
                    _ => ""
                };
                rows.Add(row);
            }

            // ***** NEW: show only rows that have Actual *****
            rows = rows.Where(r => r.Actual > 0).ToList();

            // ***** NEW: recompute totals from the filtered rows *****
            var totBudget = rows.Sum(r => r.Budget);
            var totActual = rows.Sum(r => r.Actual);
            var totVar = totActual - totBudget;
            decimal? totPct = totBudget == 0
                ? (totActual == 0 ? (decimal?)null : 100m)
                : Math.Round(totActual / totBudget * 100m, 2);

            var totals = new ShiftBudgetTotalsVm
            {
                Budget = totBudget,   // We won’t display this in the UI now
                Actual = totActual,
                Variance = totVar,
                AttainmentPct = totPct,
                Grade = totPct.HasValue ? (totPct.Value >= 100 ? "Green" : totPct.Value >= 95 ? "Yellow" : "Red") : "N/A"
            };

            var vm = new ShiftBudgetReportPageVm
            {
                Query = q,
                Totals = totals,
                Rows = rows,
                Items = await LoadItemsAsync(client, logger)
            };

            return View(vm);
        }


        // ------------ helpers -------------
        private static async Task<IEnumerable<SelectListItem>> LoadItemsAsync(HttpClient client, ILogger logger)
        {
            var url = "api/items?top=50";
            try
            {
                using var res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                res.EnsureSuccessStatusCode();

                await using var s = await res.Content.ReadAsStreamAsync();
                var list = await JsonSerializer.DeserializeAsync<List<ItemDto>>(
                    s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<ItemDto>();

                return list.Select(i => new SelectListItem
                {
                    Value = i.ItemNo,
                    Text = $"{i.ItemNo} — {i.Description}"
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed loading items for report filter.");
                return Enumerable.Empty<SelectListItem>();
            }
        }

        private record ItemDto(string ItemNo, string Description, string UnitOfMeasure);

        [HttpGet]
        public async Task<IActionResult> ShiftBudgetVsActualCsv([FromQuery] ShiftBudgetQueryVm query)
        {
            // Default window (match page)
            var today = DateTime.UtcNow.Date;
            var start = query.DateFrom?.ToDateTime(TimeOnly.MinValue)
                        ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var end = query.DateTo?.ToDateTime(TimeOnly.MinValue) ?? start.AddDays(6);

            var q = new ShiftBudgetQueryVm
            {
                DateFrom = DateOnly.FromDateTime(start),
                DateTo = DateOnly.FromDateTime(end),
                Shift = string.IsNullOrWhiteSpace(query.Shift) ? null : query.Shift,
                ItemNo = string.IsNullOrWhiteSpace(query.ItemNo) ? null : query.ItemNo
            };

            var client = http.CreateClient("ShiftApi");

            var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);
            qs["dateFrom"] = q.DateFrom?.ToString("yyyy-MM-dd");
            qs["dateTo"] = q.DateTo?.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(q.Shift)) qs["shift"] = q.Shift;
            if (!string.IsNullOrWhiteSpace(q.ItemNo)) qs["itemNo"] = q.ItemNo;

            var url = $"api/reports/shift-budget-vs-actual?{qs}";
            HttpResponseMessage res;
            try
            {
                res = await client.GetAsync(url);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export report {Url}", url);
                return BadRequest("Failed to run report for export.");
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            // Build rows
            var rows = new List<ShiftBudgetRowVm>();
            foreach (var r in root.GetProperty("rows").EnumerateArray())
            {
                rows.Add(new ShiftBudgetRowVm
                {
                    Date = DateOnly.Parse(r.GetProperty("date").GetString()!),
                    Shift = r.GetProperty("shift").GetString()!,
                    Budget = r.GetProperty("budget").GetDecimal(),
                    Actual = r.GetProperty("actual").GetDecimal(),
                    Variance = r.GetProperty("variance").GetDecimal(),
                    AttainmentPct = r.GetProperty("attainmentPct").ValueKind == JsonValueKind.Null
                                        ? null : r.GetProperty("attainmentPct").GetDecimal(),
                    Grade = r.GetProperty("grade").GetString() ?? "N/A"
                });
            }

            // Only rows with actual
            rows = rows.Where(r => r.Actual > 0).ToList();

            // Recompute totals across filtered rows
            var totBudget = rows.Sum(r => r.Budget);
            var totActual = rows.Sum(r => r.Actual);
            var totVar = totActual - totBudget;
            decimal? totPct = totBudget == 0
                ? (totActual == 0 ? (decimal?)null : 100m)
                : Math.Round(totActual / totBudget * 100m, 2);

            // Build CSV
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            // Header
            sb.AppendLine("Date,Shift,Budget,Actual,Variance,Attainment %,Grade");

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    r.Date.ToString("yyyy-MM-dd"),
                    Csv(r.Shift),
                    r.Budget.ToString(inv),
                    r.Actual.ToString(inv),
                    r.Variance.ToString(inv),
                    r.AttainmentPct.HasValue ? r.AttainmentPct.Value.ToString(inv) : "",
                    Csv(r.Grade)
                ));
            }

            // Totals row (optional, helpful)
            sb.AppendLine(string.Join(",",
                "TOTALS", "",
                totBudget.ToString(inv),
                totActual.ToString(inv),
                totVar.ToString(inv),
                totPct.HasValue ? totPct.Value.ToString(inv) : "",
                "" // grade omitted
            ));

            static string Csv(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                // Quote if contains comma/quote/newline
                if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                    return "\"" + s.Replace("\"", "\"\"") + "\"";
                return s;
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"shift-budget-vs-actual_{q.DateFrom:yyyy-MM-dd}_to_{q.DateTo:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
