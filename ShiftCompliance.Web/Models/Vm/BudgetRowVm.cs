namespace ShiftCompliance.Web.Models.Vm
{
    public class BudgetRowVm
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string Shift { get; set; } = "";
        public decimal TargetQty { get; set; }
        public string? Remark { get; set; }
    }
}
