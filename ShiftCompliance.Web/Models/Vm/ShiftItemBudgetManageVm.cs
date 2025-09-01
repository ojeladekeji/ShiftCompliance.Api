using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShiftCompliance.Web.Models.Vm
{
    public class ShiftItemBudgetManageVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? Shift { get; set; }
        public string? ItemNo { get; set; }

        public bool ActiveOnly { get; set; } = false;

        public BudgetCreateVm NewBudget { get; set; } = new();
        public List<ShiftItemBudgetRowVm> Rows { get; set; } = new();
        public IEnumerable<SelectListItem> Items { get; set; } = Enumerable.Empty<SelectListItem>();

        // --- Paging ---
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }


}
