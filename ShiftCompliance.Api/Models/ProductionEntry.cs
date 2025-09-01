namespace ShiftCompliance.Api.Models;

public enum ProductionStatus { Open = 0, Posted = 1, Closed = 2 }

public class ProductionEntry
{
    public int Id { get; set; }
    public string No { get; set; } = default!;                // e.g. "P-0001"
    public string Description { get; set; } = "";
    public string Shift { get; set; } = "Morning";            // Morning | Afternoon | Night
    public string ShiftSupervisor { get; set; } = "";
    public DateTime? PostingDateUtc { get; set; }
    public string Remark { get; set; } = "";
    public ProductionStatus Status { get; set; } = ProductionStatus.Open;

    // Image + analysis result
    public string? ImagePath { get; set; }
    public bool? IsCompliant { get; set; }
    public float? ComplianceScore { get; set; }

    public List<ProductionEntryLine> Lines { get; set; } = new();
}
