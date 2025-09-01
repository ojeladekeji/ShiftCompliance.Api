namespace ShiftCompliance.Api.Models;

public class ProductionEntryLine
{
    public int Id { get; set; }
    public int ProductionEntryId { get; set; }
    public ProductionEntry ProductionEntry { get; set; } = default!;

    public int LineNo { get; set; }                 // 10000, 20000...
    public string ItemNo { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "PCS";
    public int DowntimeMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public int SafetyIncidents { get; set; }
    public string Remark { get; set; } = "";
}
