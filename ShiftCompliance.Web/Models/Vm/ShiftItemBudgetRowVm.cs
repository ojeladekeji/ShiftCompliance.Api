namespace ShiftCompliance.Web.Models.Vm
{
    public class ShiftItemBudgetRowVm
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string Shift { get; set; } = "";
        public string ItemNo { get; set; } = "";
        public decimal TargetQty { get; set; }
        public string? Remark { get; set; }
    }
}
