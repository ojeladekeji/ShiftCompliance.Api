namespace ShiftCompliance.Api.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string ItemNo { get; set; } = default!;          // unique
        public string Description { get; set; } = "";
        public string UnitOfMeasure { get; set; } = "PCS";
        public decimal? StandardCost { get; set; }              // optional
        public bool IsActive { get; set; } = true;
    }
}
