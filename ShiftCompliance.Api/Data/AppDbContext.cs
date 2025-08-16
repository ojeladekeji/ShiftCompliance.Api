using Microsoft.EntityFrameworkCore;
using ShiftCompliance.Api.Models;

namespace ShiftCompliance.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<ShiftLog> ShiftLogs => Set<ShiftLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShiftLog>(e =>
            {
                e.HasIndex(x => x.TimestampUtc);
                e.HasIndex(x => new { x.Shift, x.TimestampUtc });
                e.HasIndex(x => new { x.Operator, x.TimestampUtc });
                e.Property(x => x.Operator).HasMaxLength(128);
                e.Property(x => x.Shift).HasMaxLength(32);
            });
        }
    }
}
