namespace ShiftCompliance.Api.Models
{
    public class ShiftLog
    {
        public int Id { get; set; }
        public string Operator { get; set; } = default!;
        public string Shift { get; set; } = default!; // Morning | Afternoon | Night
        public DateTime TimestampUtc { get; set; }
        public string ImagePath { get; set; } = default!;
        public bool IsCompliant { get; set; }
        public float Score { get; set; } // 0..1
    }
}
