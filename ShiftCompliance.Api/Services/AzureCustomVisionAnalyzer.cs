namespace ShiftCompliance.Api.Services
{
    public class AzureCustomVisionAnalyzer(HttpClient http) : IImageAnalyzer
    {
        private readonly string _url = "https://<region>.api.cognitive.microsoft.com/customvision/v3.0/Prediction/<projectId>/classify/iterations/<publishedName>/image";
        private readonly string _predictionKey = "<your-key>";

        public async Task<ComplianceResult> AnalyzeAsync(string imagePath, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            content.Headers.Add("Prediction-Key", _predictionKey);
            content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(imagePath, ct)), "imageData", Path.GetFileName(imagePath));

            var res = await http.PostAsync(_url, content, ct);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<CustomVisionResponse>(cancellationToken: ct);

            var best = json!.predictions.OrderByDescending(p => p.probability).First();
            bool compliant = string.Equals(best.tagName, "compliant", StringComparison.OrdinalIgnoreCase);
            return new ComplianceResult(compliant, (float)best.probability);
        }

        private record CustomVisionPrediction(string tagName, double probability);
        private record CustomVisionResponse(List<CustomVisionPrediction> predictions);
    }

}
