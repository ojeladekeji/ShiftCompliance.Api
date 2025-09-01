using Microsoft.EntityFrameworkCore;

namespace ShiftCompliance.Api.Services
{
    public interface IStorageService
    {
        // Saves and returns a WEB path (e.g., "/uploads/20250819-...-photo.jpg")
        Task<string> SaveAsync(Stream stream, string fileName, CancellationToken ct = default);

        // Map a web path ("/uploads/..") to a physical path so analyzers can read the file
        string MapWebPathToPhysical(string webPath);

        // Optional helpers
        Task<Stream> OpenReadAsync(string webPath, CancellationToken ct = default);
        Task DeleteAsync(string webPath, CancellationToken ct = default);

        // NEW: save with a specific file name (e.g., "P-0003.jpg") and return WEB path ("/uploads/P-0003.jpg")
        Task<string> SaveAsAsync(Stream stream, string desiredFileName, CancellationToken ct = default);

    }
}
