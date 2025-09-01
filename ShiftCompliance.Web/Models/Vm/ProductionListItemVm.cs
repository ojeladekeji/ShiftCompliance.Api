namespace ShiftCompliance.Web.Models.Vm
{
    public class ProductionListItemVm
    {
        public int Id { get; set; }
        public string? No { get; set; }
        public string? Description { get; set; }
        public string? Shift { get; set; }
        public string? ShiftSupervisor { get; set; }
        public DateTime? PostingDateUtc { get; set; }
        public bool? IsCompliant { get; set; }
        public float? ComplianceScore { get; set; }
    }
}
