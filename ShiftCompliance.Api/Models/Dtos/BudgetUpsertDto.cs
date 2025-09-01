namespace ShiftCompliance.Api.Models.Dtos
{
    public record BudgetUpsertDto
    (
        DateOnly Date,
        string Shift,
        string ItemNo,
        decimal TargetQty,
        string? Remark
);
}
