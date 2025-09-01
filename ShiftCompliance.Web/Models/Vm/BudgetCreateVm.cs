using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ShiftCompliance.Web.Models.Vm
{
    public class BudgetCreateVm
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public string Shift { get; set; } = "Morning"; // Morning | Afternoon | Night

        [Required]
        [Display(Name = "Item No")]
        public string ItemNo { get; set; } = "";

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Target Quantity")]
        public decimal TargetQty { get; set; }

        public string? Remark { get; set; }

        public IEnumerable<SelectListItem>? Items { get; set; }
    }
}
