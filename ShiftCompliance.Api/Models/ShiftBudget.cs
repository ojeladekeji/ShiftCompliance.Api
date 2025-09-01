namespace ShiftCompliance.Api.Models
{
    public class ShiftBudget
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }          // e.g. 2025-08-19
        public string Shift { get; set; } = "";     // "Morning" | "Afternoon" | "Night"
        public decimal TargetQty { get; set; }      // budgeted quantity for that shift/day
        public string? Remark { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
