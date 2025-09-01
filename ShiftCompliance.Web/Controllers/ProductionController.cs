using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftCompliance.Web.Models;
using ShiftCompliance.Web.Models.Vm;
using System.Text.Json;
using System.Web;

namespace ShiftCompliance.Web.Controllers
{
    public class ProductionController(IHttpClientFactory http) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = http.CreateClient("ShiftApi");

            // supervisors
            var res = await client.GetAsync("api/supervisors?top=50");
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var sups = System.Text.Json.JsonSerializer.Deserialize<List<SupervisorDto>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var vm = new CreateVm
            {
                Lines = new() { new CreateLineVm { LineNo = 10000 } },
                PostingDateLocal = DateTime.Now, // default to now
                Supervisors = sups.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList()
            };

            return View(vm);
        }

        private record SupervisorDto(int Id, string Name);


        [HttpPost]
        [IgnoreAntiforgeryToken]   // 
        public async Task<IActionResult> Create(CreateVm vm)
        {
            if (vm.Image is null) ModelState.AddModelError("", "Please attach an image.");
            if (vm.Lines is null || vm.Lines.Count == 0) ModelState.AddModelError("", "Add at least one line.");

            if (!ModelState.IsValid)
            {
                // re-populate supervisors if invalid
                var client0 = http.CreateClient("ShiftApi");
                var res0 = await client0.GetAsync("api/supervisors?top=50");
                var json0 = await res0.Content.ReadAsStringAsync();
                var sups0 = System.Text.Json.JsonSerializer.Deserialize<List<SupervisorDto>>(json0,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                vm.Supervisors = sups0.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();

                return View(vm);
            }

            var client = http.CreateClient("ShiftApi");

            var form = new MultipartFormDataContent();
            form.Add(new StringContent(vm.No ?? ""), "No");
            form.Add(new StringContent(vm.Description ?? ""), "Description");
            form.Add(new StringContent(vm.Shift ?? "Morning"), "Shift");
            if (vm.ShiftSupervisorId.HasValue)
                form.Add(new StringContent(vm.ShiftSupervisorId.Value.ToString()), "ShiftSupervisorId"); // NEW
            else if (!string.IsNullOrWhiteSpace(vm.ShiftSupervisor))
                form.Add(new StringContent(vm.ShiftSupervisor), "ShiftSupervisor");

            if (vm.PostingDateLocal.HasValue)
                form.Add(new StringContent(vm.PostingDateLocal.Value.ToString("o")), "PostingDateLocal");
            form.Add(new StringContent(vm.Remark ?? ""), "Remark");

            var linesJson = System.Text.Json.JsonSerializer.Serialize(vm.Lines);
            form.Add(new StringContent(linesJson), "LinesJson");

            if (vm.Image is not null)
                form.Add(new StreamContent(vm.Image.OpenReadStream()), "Image", vm.Image.FileName);

            var res = await client.PostAsync("api/production", form);
            ViewBag.Result = await res.Content.ReadAsStringAsync();

            // repopulate supervisors for re-render
            var resSup = await client.GetAsync("api/supervisors?top=50");
            var jsonSup = await resSup.Content.ReadAsStringAsync();
            var sups = System.Text.Json.JsonSerializer.Deserialize<List<SupervisorDto>>(jsonSup,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            vm.Supervisors = sups.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();

            return View(vm);
        }

       


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var client = http.CreateClient("ShiftApi");

            var res = await client.GetAsync($"api/production/{id}");
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            var dto = System.Text.Json.JsonSerializer.Deserialize<ProductionDetailsDto>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            // Build absolute URL for the image (so it loads from the API host/port)
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(dto.ImagePath))
            {
                // dto.ImagePath is like "/uploads/P-0005.jpg"
                var baseUri = client.BaseAddress ?? new Uri("https://localhost:7255/"); // fallback
                imageUrl = new Uri(baseUri, dto.ImagePath).ToString();
            }

            var vm = new ProductionDetailsVm
            {
                Id = dto.Id,
                No = dto.No,
                Description = dto.Description,
                Shift = dto.Shift,
                ShiftSupervisor = dto.ShiftSupervisor,
                PostingDateUtc = dto.PostingDateUtc,
                IsCompliant = dto.IsCompliant,
                ComplianceScore = dto.ComplianceScore,
                Lines = dto.Lines.Select(l => new ProductionLineVm
                {
                    LineNo = l.LineNo,
                    ItemNo = l.ItemNo,
                    Quantity = l.Quantity,
                    UnitOfMeasure = l.UnitOfMeasure,
                    DowntimeMinutes = l.DowntimeMinutes,
                    OvertimeHours = l.OvertimeHours,
                    SafetyIncidents = l.SafetyIncidents,
                    Remark = l.Remark
                }).ToList(),
                ImageUrl = imageUrl                 //  instance assignment
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ProductionListQueryVm query)
        {
            var client = http.CreateClient("ShiftApi");

            var qp = HttpUtility.ParseQueryString(string.Empty);
            qp["page"] = (query.Page <= 0 ? 1 : query.Page).ToString();
            qp["pageSize"] = (query.PageSize <= 0 ? 20 : query.PageSize).ToString();

            if (query.DateFrom.HasValue)
                qp["dateFrom"] = DateOnly.FromDateTime(query.DateFrom.Value).ToString("yyyy-MM-dd");
            if (query.DateTo.HasValue)
                qp["dateTo"] = DateOnly.FromDateTime(query.DateTo.Value).ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(query.Shift))
                qp["shift"] = query.Shift;
            if (!string.IsNullOrWhiteSpace(query.Supervisor))
                qp["supervisor"] = query.Supervisor;

            bool? compliant = query.Compliance?.ToLowerInvariant() switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
            if (compliant.HasValue)
                qp["compliant"] = compliant.Value.ToString().ToLowerInvariant();

            var url = $"api/production?{qp}";
            var res = await client.GetAsync(url);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var page = root.GetProperty("page").GetInt32();
            var pageSize = root.GetProperty("pageSize").GetInt32();
            var total = root.GetProperty("total").GetInt32();
            var totalPages = root.GetProperty("totalPages").GetInt32();

            var items = new List<ProductionListItemVm>();
            foreach (var it in root.GetProperty("items").EnumerateArray())
            {
                items.Add(new ProductionListItemVm
                {
                    Id = it.GetProperty("id").GetInt32(),
                    No = it.GetProperty("no").GetString(),
                    Description = it.GetProperty("description").GetString(),
                    Shift = it.GetProperty("shift").GetString(),
                    ShiftSupervisor = it.GetProperty("shiftSupervisor").GetString(),
                    PostingDateUtc = it.GetProperty("postingDateUtc").ValueKind == JsonValueKind.Null
                                        ? null
                                        : it.GetProperty("postingDateUtc").GetDateTime(),
                    IsCompliant = it.GetProperty("isCompliant").ValueKind == JsonValueKind.Null
                                        ? null
                                        : it.GetProperty("isCompliant").GetBoolean(),
                    ComplianceScore = it.GetProperty("complianceScore").ValueKind == JsonValueKind.Null
                                        ? null
                                        : it.GetProperty("complianceScore").GetSingle()
                });
            }

            var vm = new ProductionListPageVm
            {
                Query = query,
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages
            };

            return View(vm);
        }


    }
}
