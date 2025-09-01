namespace ShiftCompliance.Web.Models.Vm
{
    public class BudgetManageVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public BudgetUpsertVm NewBudget { get; set; } = new();
        public List<BudgetRowVm> Rows { get; set; } = new();
    }
}
