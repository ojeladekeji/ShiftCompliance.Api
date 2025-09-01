using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShiftCompliance.Api.Models.Dtos
{
    public class ProductionCreateDto
    {
        // Header
        public string? No { get; set; }

        [Required]
        public string Shift { get; set; } = "Morning";

        /// <summary>
        /// Preferred: selected supervisor from lookup.
        /// API will resolve this to a name and store it in the header.
        /// </summary>
        public int? ShiftSupervisorId { get; set; }

        /// <summary>
        /// Legacy/optional free-text supervisor name (used if Id is not provided).
        /// </summary>
        public string? ShiftSupervisor { get; set; }

        public string? Description { get; set; }
        public string? Remark { get; set; }

        /// <summary>
        /// Client sends local time (HTML datetime-local).
        /// API converts to UTC.
        /// </summary>
        public DateTime? PostingDateLocal { get; set; }

        /// <summary>
        /// Optional photo for compliance analysis.
        /// </summary>
        public IFormFile? Image { get; set; }

        /// <summary>
        /// Lines posted either as a structured list (JSON body in multipart)
        /// or via a single "LinesJson" field (the API already handles both).
        /// </summary>
        [MinLength(1, ErrorMessage = "At least one line is required.")]
        public List<ProductionLineDto>? Lines { get; set; }
    }

    public class ProductionLineDto
    {
        [Required]
        public int LineNo { get; set; }

        [Required]
        public string ItemNo { get; set; } = "";

        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// If empty, API will default from Item.UnitOfMeasure.
        /// </summary>
        public string? UnitOfMeasure { get; set; }

        public int DowntimeMinutes { get; set; }
        public decimal OvertimeHours { get; set; }
        public int SafetyIncidents { get; set; }
        public string? Remark { get; set; }
    }
}
