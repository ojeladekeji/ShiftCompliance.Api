using System.Security.Cryptography;

namespace ShiftCompliance.Api.Services
{
    public class LocalStorageService(IWebHostEnvironment env) : IStorageService
    {
        private readonly string _root = Path.Combine(env.ContentRootPath, "uploads");

        public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
        {
            Directory.CreateDirectory(_root);
            var ext = Path.GetExtension(fileName);
            var name = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{RandomNumberGenerator.GetInt32(int.MaxValue)}{ext}";
            var path = Path.Combine(_root, name);
            using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
            await content.CopyToAsync(fs, ct);
            return path; // store absolute path; expose only summaries externally
        }
    }
}
