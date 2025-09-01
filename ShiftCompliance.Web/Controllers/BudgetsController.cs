using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using ShiftCompliance.Web.Models.Vm;

namespace ShiftCompliance.Web.Controllers
{
    public class BudgetsController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<BudgetsController> _logger;

        public BudgetsController(IHttpClientFactory http, ILogger<BudgetsController> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ===========================
        // SHIFT-LEVEL (whole shift/day)
        // ===========================

        // GET: /Budgets/Manage?from=2025-08-18&to=2025-08-24
        [HttpGet]
        public async Task<IActionResult> Manage(DateTime? from, DateTime? to)
        {
            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var end = to?.Date ?? start.AddDays(6);

            var vm = new BudgetManageVm
            {
                From = start,
                To = end,
                NewBudget = new BudgetUpsertVm
                {
                    Date = start,
                    Shift = "Morning",
                    TargetQty = 0
                },
                Rows = new List<BudgetRowVm>()
            };

            var client = _http.CreateClient("ShiftApi");
            var url = $"api/budgets/shift?from={vm.From:yyyy-MM-dd}&to={vm.To:yyyy-MM-dd}";
            HttpResponseMessage res;

            try
            {
                res = await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling {Url}", url);
                ViewBag.Error = "Failed to load budgets (network error).";
                return View(vm);
            }

            if (!res.IsSuccessStatusCode)
            {
                ViewBag.Error = $"Failed to load budgets ({(int)res.StatusCode})";
                return View(vm);
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                vm.Rows.Add(new BudgetRowVm
                {
                    Id = el.GetProperty("id").GetInt32(),
                    Date = DateOnly.Parse(el.GetProperty("date").GetString()!),
                    Shift = el.GetProperty("shift").GetString()!,
                    TargetQty = el.GetProperty("targetQty").GetDecimal(),
                    Remark = el.TryGetProperty("remark", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() : ""
                });
            }

            return View(vm);
        }

        // POST: /Budgets/Upsert
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(BudgetUpsertVm vm)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(Manage));

            var client = _http.CreateClient("ShiftApi");
            var payload = new
            {
                date = DateOnly.FromDateTime(vm.Date),
                shift = vm.Shift,
                targetQty = vm.TargetQty,
                remark = vm.Remark,
                isActive = true
            };

            var res = await client.PostAsync(
                "api/budgets/shift",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            TempData[res.IsSuccessStatusCode ? "Ok" : "Err"] =
                res.IsSuccessStatusCode ? "Budget saved." : $"Save failed ({(int)res.StatusCode}).";

            return RedirectToAction(nameof(Manage), new
            {
                from = vm.Date.AddDays(-(int)vm.Date.DayOfWeek + (int)DayOfWeek.Monday),
                to = vm.Date.AddDays(6)
            });
        }

        // POST: /Budgets/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, DateTime from, DateTime to)
        {
            var client = _http.CreateClient("ShiftApi");
            var res = await client.DeleteAsync($"api/budgets/shift/{id}");
            TempData[res.IsSuccessStatusCode ? "Ok" : "Err"] =
                res.IsSuccessStatusCode ? "Deleted." : $"Delete failed ({(int)res.StatusCode}).";
            return RedirectToAction(nameof(Manage), new { from, to });
        }

        // ===========================
        // ITEM-LEVEL (per item + shift)
        // ===========================

        // GET: /Budgets/ManageItems?from=...&to=...&shift=...&itemNo=...&activeOnly=true&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> ManageItems(
            DateTime? from,
            DateTime? to,
            string? shift,
            string? itemNo,
            bool activeOnly = false,
            int page = 1,
            int pageSize = 20)
        {
            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var end = to?.Date ?? start.AddDays(6);

            var vm = new ShiftItemBudgetManageVm
            {
                From = start,
                To = end,
                Shift = shift ?? "",
                ItemNo = itemNo ?? "",
                ActiveOnly = activeOnly,
                NewBudget = new BudgetCreateVm
                {
                    Date = start,
                    Shift = "Morning",
                    TargetQty = 0
                },
                Items = await LoadItemsAsync(),
                Page = page,
                PageSize = pageSize
            };

            var client = _http.CreateClient("ShiftApi");
            var url = new StringBuilder("api/budgets/items?")
                .Append($"from={vm.From:yyyy-MM-dd}&to={vm.To:yyyy-MM-dd}")
                .Append(string.IsNullOrWhiteSpace(vm.Shift) ? "" : $"&shift={Uri.EscapeDataString(vm.Shift)}")
                .Append(string.IsNullOrWhiteSpace(vm.ItemNo) ? "" : $"&itemNo={Uri.EscapeDataString(vm.ItemNo)}")
                .Append($"&page={vm.Page}&pageSize={vm.PageSize}");

            if (vm.ActiveOnly) url.Append("&onlyActive=true");

            HttpResponseMessage res;
            try
            {
                res = await client.GetAsync(url.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling {Url}", url.ToString());
                ViewBag.Error = "Failed to load item budgets (network error).";
                return View(vm);
            }

            if (!res.IsSuccessStatusCode)
            {
                ViewBag.Error = $"Failed to load item budgets ({(int)res.StatusCode})";
                return View(vm);
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            vm.Total = doc.RootElement.GetProperty("total").GetInt32();
            vm.TotalPages = doc.RootElement.GetProperty("totalPages").GetInt32();

            foreach (var el in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                vm.Rows.Add(new ShiftItemBudgetRowVm
                {
                    Id = el.GetProperty("id").GetInt32(),
                    Date = DateOnly.Parse(el.GetProperty("date").GetString()!),
                    Shift = el.GetProperty("shift").GetString()!,
                    ItemNo = el.GetProperty("itemNo").GetString()!,
                    TargetQty = el.GetProperty("targetQty").GetDecimal(),
                    Remark = el.TryGetProperty("remark", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() : ""
                });
            }

            return View(vm);
        }

        // POST: /Budgets/UpsertItem
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertItem(BudgetCreateVm vm, bool activeOnly = false)
        {
            if (!ModelState.IsValid)
            {
                vm.Items = await LoadItemsAsync();
                return View("ManageItems", new ShiftItemBudgetManageVm
                {
                    From = vm.Date,
                    To = vm.Date.AddDays(6),
                    Shift = vm.Shift,
                    ItemNo = vm.ItemNo,
                    ActiveOnly = activeOnly,
                    NewBudget = vm,
                    Items = vm.Items
                });
            }

            if (string.IsNullOrWhiteSpace(vm.ItemNo))
            {
                ModelState.AddModelError(nameof(vm.ItemNo), "Please select an item.");
                vm.Items = await LoadItemsAsync();
                return View("ManageItems", new ShiftItemBudgetManageVm
                {
                    From = vm.Date,
                    To = vm.Date.AddDays(6),
                    Shift = vm.Shift,
                    ItemNo = vm.ItemNo,
                    ActiveOnly = activeOnly,
                    NewBudget = vm,
                    Items = vm.Items
                });
            }

            var client = _http.CreateClient("ShiftApi");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Use ISO for date to avoid serialization quirks
            var payload = new
            {
                date = vm.Date.ToString("yyyy-MM-dd"),
                shift = vm.Shift,
                itemNo = vm.ItemNo,
                targetQty = vm.TargetQty,
                remark = vm.Remark
            };

            var res = await client.PostAsync(
                "api/budgets/upsert",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                TempData["Err"] = $"Save failed ({(int)res.StatusCode}). {body}";
            }
            else
            {
                TempData["Ok"] = "Item budget saved.";
            }

            return RedirectToAction(nameof(ManageItems), new
            {
                from = vm.Date,
                to = vm.Date.AddDays(6),
                shift = vm.Shift,
                itemNo = vm.ItemNo,
                activeOnly
            });
        }

        // POST: /Budgets/DeleteItem/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id, DateTime from, DateTime to, string? shift, string? itemNo, bool activeOnly = false)
        {
            var client = _http.CreateClient("ShiftApi");
            var res = await client.DeleteAsync($"api/budgets/items/{id}");
            TempData[res.IsSuccessStatusCode ? "Ok" : "Err"] =
                res.IsSuccessStatusCode ? "Deleted." : $"Delete failed ({(int)res.StatusCode}).";

            return RedirectToAction(nameof(ManageItems), new { from, to, shift, itemNo, activeOnly });
        }

        // ===========================
        // Helpers
        // ===========================
        private async Task<IEnumerable<SelectListItem>> LoadItemsAsync()
        {
            var client = _http.CreateClient("ShiftApi");
            var url = "api/items?top=50";
            _logger.LogInformation("Calling API: {Uri}", new Uri(client.BaseAddress!, url));

            HttpResponseMessage res;
            try
            {
                res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP failure calling items endpoint.");
                throw new InvalidOperationException(
                    $"Items endpoint unreachable at {new Uri(client.BaseAddress!, url)}: {ex.Message}", ex);
            }

            if ((int)res.StatusCode is 301 or 302 or 307 or 308)
            {
                var loc = res.Headers.Location?.ToString() ?? "(none)";
                _logger.LogError("Items endpoint redirected to {Location}. Check base address/scheme.", loc);
                throw new InvalidOperationException($"Items endpoint redirected to {loc}. Fix BaseAddress/scheme.");
            }

            res.EnsureSuccessStatusCode();

            await using var s = await res.Content.ReadAsStreamAsync();
            var items = await JsonSerializer.DeserializeAsync<List<ItemDto>>(s,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<ItemDto>();

            return items.Select(it => new SelectListItem
            {
                Value = it.ItemNo,
                Text = $"{it.ItemNo} — {it.Description}"
            });
        }

        private record ItemDto(string ItemNo, string Description, string UnitOfMeasure);
    }
}
