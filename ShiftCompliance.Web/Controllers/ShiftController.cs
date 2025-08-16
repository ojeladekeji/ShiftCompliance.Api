using Microsoft.AspNetCore.Mvc;

namespace ShiftCompliance.Web.Controllers
{
    public class ShiftController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public ShiftController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile image, string operatorName, string shift, DateTime? timestampLocal)
        {
            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError("", "Please select an image.");
                return View();
            }

            var client = _clientFactory.CreateClient("ShiftApi");
            using var content = new MultipartFormDataContent();
            using var fileStream = image.OpenReadStream();
            content.Add(new StreamContent(fileStream), "Image", image.FileName);
            content.Add(new StringContent(operatorName ?? ""), "Operator");
            content.Add(new StringContent(shift ?? ""), "Shift");
            if (timestampLocal.HasValue)
                content.Add(new StringContent(timestampLocal.Value.ToString("o")), "TimestampLocal");

            var response = await client.PostAsync("api/shifts/upload", content);
            ViewBag.Result = await response.Content.ReadAsStringAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DailySummary(DateTime? day)
        {
            ViewBag.Day = day?.ToString("yyyy-MM-dd") ?? "";
            var client = _clientFactory.CreateClient("ShiftApi");
            var dateQuery = day.HasValue ? $"?day={day:yyyy-MM-dd}" : "";
            var result = await client.GetStringAsync($"api/shifts/daily-summary{dateQuery}");
            ViewBag.SummaryJson = result;
            return View();
        }

    }
}
