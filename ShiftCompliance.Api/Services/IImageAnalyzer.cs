namespace ShiftCompliance.Api.Services
{
    public record ComplianceResult(bool IsCompliant, float Score);

    public interface IImageAnalyzer
    {
        Task<ComplianceResult> AnalyzeAsync(string imagePath, CancellationToken ct = default);
    }
}
