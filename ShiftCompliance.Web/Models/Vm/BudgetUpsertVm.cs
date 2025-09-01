namespace ShiftCompliance.Web.Models.Vm
{
    public class BudgetUpsertVm
    {
        public DateTime Date { get; set; }
        public string Shift { get; set; } = "Morning";
        public decimal TargetQty { get; set; }
        public string? Remark { get; set; }
    }
}
