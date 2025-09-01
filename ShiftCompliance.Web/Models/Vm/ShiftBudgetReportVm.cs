namespace ShiftCompliance.Web.Models.Vm
{
    public class ShiftBudgetReportVm
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalActual { get; set; }
        public decimal Variance { get; set; }
        public decimal? AttainmentPct { get; set; }
        public List<ShiftBudgetRowVm> Rows { get; set; } = new();
    }
}
