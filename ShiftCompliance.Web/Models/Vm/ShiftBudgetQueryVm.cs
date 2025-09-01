namespace ShiftCompliance.Web.Models.Vm
{
    public class ShiftBudgetQueryVm
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public string? Shift { get; set; }       // Morning/Afternoon/Night or null
        public string? ItemNo { get; set; }      // optional filter by item
    }
}
