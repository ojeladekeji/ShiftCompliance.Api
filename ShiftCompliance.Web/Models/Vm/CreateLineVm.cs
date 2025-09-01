namespace ShiftCompliance.Web.Models.Vm
{
    public class CreateLineVm
    {
        public int LineNo { get; set; }                        // 10000, 20000, ...
        public string ItemNo { get; set; } = "";
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = "PCS";
        public int DowntimeMinutes { get; set; }
        public decimal OvertimeHours { get; set; }
        public int SafetyIncidents { get; set; }
        public string? Remark { get; set; }
    }
}
