using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ShiftCompliance.Api.Services
{
    /// <summary>
    /// Detects red "X" or green "check" markers by color coverage in image corners.
    /// Robust to background content because it only inspects corner regions
    /// and uses HSV color masks (red/green).
    /// </summary>
    public class MarkerColorAnalyzer : IImageAnalyzer
    {
        public Task<ComplianceResult> AnalyzeAsync(string imagePath, CancellationToken ct = default)
        {
            using Image<Rgba32> img = Image.Load<Rgba32>(imagePath);

            // choose a corner crop size proportional to image
            int side = Math.Max(60, Math.Min(img.Width, img.Height) / 4);

            var corners = new[]
            {
                new Rectangle(0, 0, side, side),                                             // TL
                new Rectangle(img.Width - side, 0, side, side),                              // TR
                new Rectangle(0, img.Height - side, side, side),                             // BL
                new Rectangle(img.Width - side, img.Height - side, side, side)               // BR
            };

            double totalRed = 0, totalGreen = 0;

            foreach (var rect in corners)
            {
                using var region = img.Clone(c => c.Crop(rect).Resize(128, 128)); // normalize
                var (redCov, greenCov) = ColorCoverage(region);
                totalRed += redCov;
                totalGreen += greenCov;
            }

            bool compliant = totalGreen >= totalRed;
            // score = dominant coverage in [0..1]
            float score = (float)Math.Clamp(Math.Max(totalGreen, totalRed), 0, 1);

            return Task.FromResult(new ComplianceResult(compliant, score));
        }

        /// <summary>
        /// Returns fraction of pixels that are "red" and "green" (HSV masks) in [0..1].
        /// </summary>
        private static (double red, double green) ColorCoverage(Image<Rgba32> img)
        {
            long redCount = 0, greenCount = 0, total = 0;

            for (int y = 0; y < img.Height; y++)
            {
                var row = img.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < img.Width; x++)
                {
                    var p = row[x];
                    Rgba32ToHsv(p, out double h, out double s, out double v);

                    // ignore very dark or low-sat pixels
                    if (v < 0.25 || s < 0.35) { total++; continue; }

                    // RED: hue around 0° (wrap) -> [0..15] or [345..360]
                    bool isRed = (h <= 15 || h >= 345);

                    // GREEN: hue around 120° -> [85..150]
                    bool isGreen = (h >= 85 && h <= 150);

                    if (isRed) redCount++;
                    if (isGreen) greenCount++;
                    total++;
                }
            }

            if (total == 0) return (0, 0);
            return (redCount / (double)total, greenCount / (double)total);
        }

        /// <summary>
        /// Convert RGB to HSV (H in degrees 0..360, S/V 0..1).
        /// </summary>
        private static void Rgba32ToHsv(Rgba32 c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            v = max;

            double d = max - min;
            s = max == 0 ? 0 : d / max;

            if (d == 0)
            {
                h = 0;
            }
            else
            {
                if (max == r) h = 60 * (((g - b) / d) % 6);
                else if (max == g) h = 60 * (((b - r) / d) + 2);
                else h = 60 * (((r - g) / d) + 4);

                if (h < 0) h += 360;
            }
        }
    }
}
