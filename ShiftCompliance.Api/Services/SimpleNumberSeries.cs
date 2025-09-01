using Microsoft.EntityFrameworkCore;
using ShiftCompliance.Api.Data;

namespace ShiftCompliance.Api.Services;

public class SimpleNumberSeries(AppDbContext db) : INumberSeries
{
    public async Task<string> NextAsync()
    {
        var last = await db.ProductionEntries
            .OrderByDescending(x => x.Id).Select(x => x.No).FirstOrDefaultAsync();

        int n = 0;
        if (!string.IsNullOrWhiteSpace(last) && last.StartsWith("P-") &&
            int.TryParse(last.AsSpan(2), out var parsed))
            n = parsed;

        return $"P-{(n + 1).ToString("0000")}";
    }
}
