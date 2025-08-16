using Microsoft.AspNetCore.Http;


namespace ShiftCompliance.Api.Models.Dtos
{
    public class ShiftImageUploadDto
    {
        public IFormFile Image { get; set; } = default!;
        public string Operator { get; set; } = default!;
        public string Shift { get; set; } = default!; // Morning | Afternoon | Night
        public DateTime? TimestampLocal { get; set; } // optional, else server time
    }
}
