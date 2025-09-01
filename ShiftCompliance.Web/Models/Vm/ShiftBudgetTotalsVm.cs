namespace ShiftCompliance.Web.Models.Vm
{
    public class ShiftBudgetTotalsVm
    {
        public decimal Budget { get; set; }
        public decimal Actual { get; set; }
        public decimal Variance { get; set; }
        public decimal? AttainmentPct { get; set; }
        public string Grade { get; set; } = "N/A";
    }
}
