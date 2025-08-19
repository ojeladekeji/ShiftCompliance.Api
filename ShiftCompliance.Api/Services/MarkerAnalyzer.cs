using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;   // Resize/Crop
using SixLabors.ImageSharp.Advanced;     // DangerousGetPixelRowMemory

namespace ShiftCompliance.Api.Services
{
    public class MarkerAnalyzer : IImageAnalyzer
    {
        private readonly string _greenPath;
        private readonly string _redPath;

        public MarkerAnalyzer(IWebHostEnvironment env)
        {
            _greenPath = Path.Combine(env.ContentRootPath, "Markers", "green-check.png");
            _redPath = Path.Combine(env.ContentRootPath, "Markers", "red-x.png");
        }

        public async Task<ComplianceResult> AnalyzeAsync(string imagePath, CancellationToken ct = default)
        {
            using Image<Rgba32> uploaded = await Image.LoadAsync<Rgba32>(imagePath, ct);
            using Image<Rgba32> green = await Image.LoadAsync<Rgba32>(_greenPath, ct);
            using Image<Rgba32> red = await Image.LoadAsync<Rgba32>(_redPath, ct);

            // Compare against four-corner regions so small corner markers are detected
            double greenScore = RegionMatch(uploaded, green);
            double redScore = RegionMatch(uploaded, red);

            bool isCompliant = greenScore >= redScore;
            float score = (float)Math.Max(greenScore, redScore);
            return new ComplianceResult(isCompliant, score);
        }

        /// <summary>
        /// Checks four corners of the uploaded image; returns the best similarity to the marker.
        /// </summary>
        private static double RegionMatch(Image<Rgba32> uploaded, Image<Rgba32> marker)
        {
            using var resizedMarker = marker.Clone(c => c.Resize(64, 64));

            int side = Math.Min(120, Math.Min(uploaded.Width, uploaded.Height)); // crop size
            var regions = new[]
            {
                new Rectangle(0, 0, side, side),                                             // TL
                new Rectangle(uploaded.Width - side, 0, side, side),                         // TR
                new Rectangle(0, uploaded.Height - side, side, side),                        // BL
                new Rectangle(uploaded.Width - side, uploaded.Height - side, side, side)     // BR
            };

            double best = 0;
            foreach (var r in regions)
            {
                using var crop = uploaded.Clone(c => c.Crop(r).Resize(64, 64));
                double sim = Similarity(crop, resizedMarker);
                if (sim > best) best = sim;
            }
            return best;
        }

        /// <summary>
        /// Pixel similarity in [0..1] using Euclidean RGB distance.
        /// Works on ImageSharp v3 using DangerousGetPixelRowMemory (no GetPixelRowSpan).
        /// </summary>
        private static double Similarity(Image<Rgba32> a, Image<Rgba32> b)
        {
            int w = Math.Min(a.Width, b.Width);
            int h = Math.Min(a.Height, b.Height);

            double total = 0;
            long count = 0;

            for (int y = 0; y < h; y++)
            {
                var rowA = a.DangerousGetPixelRowMemory(y).Span;
                var rowB = b.DangerousGetPixelRowMemory(y).Span;

                for (int x = 0; x < w; x++)
                {
                    int dr = rowA[x].R - rowB[x].R;
                    int dg = rowA[x].G - rowB[x].G;
                    int db = rowA[x].B - rowB[x].B;

                    // max distance ≈ sqrt(3 * 255^2) = 441.6729
                    double dist = Math.Sqrt(dr * dr + dg * dg + db * db);
                    double sim = 1.0 - (dist / 441.67295593);
                    total += sim;
                    count++;
                }
            }
            return count == 0 ? 0.0 : total / count;
        }
    }
}
