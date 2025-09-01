using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShiftCompliance.Web.Models.Vm
{
    public class ShiftBudgetReportPageVm
    {
        public ShiftBudgetQueryVm Query { get; set; } = new();
        public ShiftBudgetTotalsVm Totals { get; set; } = new();
        public List<ShiftBudgetRowVm> Rows { get; set; } = new();

        public IEnumerable<SelectListItem> Items { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
