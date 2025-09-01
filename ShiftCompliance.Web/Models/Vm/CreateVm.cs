using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShiftCompliance.Web.Models.Vm
{
    public class CreateVm
    {
        public string? No { get; set; }                        // leave empty to auto-generate
        public string? Description { get; set; }
        public string? Shift { get; set; } = "Morning";
        public string? ShiftSupervisor { get; set; }
        public int? ShiftSupervisorId { get; set; }                  // NEW
        public List<SelectListItem> Supervisors { get; set; } = new(); // NEW
        public DateTime? PostingDateLocal { get; set; }
        public string? Remark { get; set; }
        public IFormFile? Image { get; set; }

        public List<CreateLineVm> Lines { get; set; } = new();
    }
}
