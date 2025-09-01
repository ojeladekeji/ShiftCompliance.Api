using System.Security.Cryptography;

namespace ShiftCompliance.Api.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _webRoot;
        private readonly string _uploadRoot;

        public LocalStorageService(IWebHostEnvironment env)
        {
            _webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _uploadRoot = Path.Combine(_webRoot, "uploads");
            Directory.CreateDirectory(_uploadRoot);
        }

        public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(fileName);
            var unique = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{RandomNumberGenerator.GetInt32(int.MaxValue)}{ext}";
            var target = Path.Combine(_uploadRoot, unique);

            using var fs = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
            await content.CopyToAsync(fs, ct);

            return $"/uploads/{unique}";
        }

        // NEW: save with a precise name (e.g., "P-0003.jpg"), auto-disambiguate if it already exists
        public async Task<string> SaveAsAsync(Stream content, string desiredFileName, CancellationToken ct = default)
        {
            var safeName = MakeSafeFileName(desiredFileName);
            var target = Path.Combine(_uploadRoot, safeName);

            // If a file with the same name exists, append a numeric suffix
            if (System.IO.File.Exists(target))
            {
                var name = Path.GetFileNameWithoutExtension(safeName);
                var ext = Path.GetExtension(safeName);

                int i = 1;
                do
                {
                    var candidate = $"{name}-{i}{ext}";
                    target = Path.Combine(_uploadRoot, candidate);
                    if (!System.IO.File.Exists(target))
                    {
                        safeName = candidate;
                        break;
                    }
                    i++;
                } while (true);
            }

            using var fs = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
            await content.CopyToAsync(fs, ct);

            return $"/uploads/{safeName}";
        }

        public string MapWebPathToPhysical(string webPath)
        {
            var rel = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_webRoot, rel);
        }

        public Task<Stream> OpenReadAsync(string webPath, CancellationToken ct = default)
        {
            var full = MapWebPathToPhysical(webPath);
            Stream s = System.IO.File.OpenRead(full);
            return Task.FromResult(s);
        }

        public Task DeleteAsync(string webPath, CancellationToken ct = default)
        {
            var full = MapWebPathToPhysical(webPath);
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            return Task.CompletedTask;
        }

        private static string MakeSafeFileName(string fileName)
        {
            var name = Path.GetFileName(fileName); // strip any path
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '-');
            return name;
        }
    }
}
