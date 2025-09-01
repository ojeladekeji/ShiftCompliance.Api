namespace ShiftCompliance.Web.Models.Vm
{
    public class ProductionDetailsVm
    {
        public int Id { get; set; }
        public string? No { get; set; }
        public string? Description { get; set; }
        public string? Shift { get; set; }
        public string? ShiftSupervisor { get; set; }
        public DateTime? PostingDateUtc { get; set; }
        public bool? IsCompliant { get; set; }
        public float? ComplianceScore { get; set; }

        public IEnumerable<ProductionLineVm> Lines { get; set; } = Enumerable.Empty<ProductionLineVm>();

        // NEW: absolute URL to the image (https://localhost:7255/uploads/...)
        public string? ImageUrl { get; set; }
    }

    public class ProductionLineVm
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
