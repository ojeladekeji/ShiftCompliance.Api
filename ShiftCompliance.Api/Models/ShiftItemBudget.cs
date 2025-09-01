namespace ShiftCompliance.Api.Models
{
    public class ShiftItemBudget
    {
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        public string Shift { get; set; } = "";   // Morning/Afternoon/Night

        public string ItemNo { get; set; } = "";

        public decimal TargetQty { get; set; }

        public string? Remark { get; set; }

        public bool IsActive { get; set; } = true;

        // --- Navigation (optional) ---
       // public Item? Item { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }
    }
}
