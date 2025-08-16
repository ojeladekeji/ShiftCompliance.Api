using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace ShiftCompliance.Api.Services
{
    public class BrightnessAnalyzer : IImageAnalyzer
    {
        private const float Threshold = 0.55f;

        public async Task<ComplianceResult> AnalyzeAsync(string imagePath, CancellationToken ct = default)
        {
            using Image<Rgba32> img = await Image.LoadAsync<Rgba32>(imagePath, ct);

            double total = 0;
            long count = 0;

            int step = Math.Max(1, Math.Min(img.Width, img.Height) / 512);

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y += step)
                {
                    var row = accessor.GetRowSpan(y);   // v3-friendly
                    for (int x = 0; x < accessor.Width; x += step)
                    {
                        var p = row[x];
                        var r = p.R / 255.0;
                        var g = p.G / 255.0;
                        var b = p.B / 255.0;
                        var luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;

                        total += luminance;
                        count++;
                    }
                }
            });

            float score = (float)(total / Math.Max(1, count));
            bool compliant = score >= Threshold;

            return new ComplianceResult(compliant, score);
        }
    }
}
