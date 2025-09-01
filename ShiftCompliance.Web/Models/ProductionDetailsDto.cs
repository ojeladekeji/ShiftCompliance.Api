namespace ShiftCompliance.Web.Models
{
    public class ProductionDetailsDto
    {
        public int Id { get; set; }
        public string? No { get; set; }
        public string? Description { get; set; }
        public string? Shift { get; set; }
        public string? ShiftSupervisor { get; set; }
        public DateTime? PostingDateUtc { get; set; }
        public string? Status { get; set; }
        public bool? IsCompliant { get; set; }
        public float? ComplianceScore { get; set; }
        public string? ImagePath { get; set; }
        public List<ProductionLineDto> Lines { get; set; } = new();
    }

    public class ProductionLineDto
    {
        public int LineNo { get; set; }
        public string? ItemNo { get; set; }
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int DowntimeMinutes { get; set; }
        public decimal OvertimeHours { get; set; }
        public int SafetyIncidents { get; set; }
        public string? Remark { get; set; }
    }
}
