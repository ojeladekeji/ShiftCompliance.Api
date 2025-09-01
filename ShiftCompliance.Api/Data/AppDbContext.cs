using Microsoft.EntityFrameworkCore;
using ShiftCompliance.Api.Models;

namespace ShiftCompliance.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<ShiftLog> ShiftLogs => Set<ShiftLog>();
        public DbSet<ProductionEntry> ProductionEntries => Set<ProductionEntry>();
        public DbSet<ProductionEntryLine> ProductionEntryLines => Set<ProductionEntryLine>();
        public DbSet<Item> Items => Set<Item>();

        public DbSet<Supervisor> Supervisors => Set<Supervisor>();

        public DbSet<ShiftBudget> ShiftBudgets => Set<ShiftBudget>();
        public DbSet<ShiftItemBudget> ShiftItemBudgets => Set<ShiftItemBudget>();




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

            modelBuilder.Entity<ProductionEntry>(e =>
            {
                e.HasIndex(x => x.No).IsUnique();
                e.Property(x => x.No).HasMaxLength(20);
                e.Property(x => x.Shift).HasMaxLength(32);
                e.Property(x => x.ShiftSupervisor).HasMaxLength(128);
                e.HasMany(x => x.Lines).WithOne(l => l.ProductionEntry)
                                       .HasForeignKey(l => l.ProductionEntryId)
                                       .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductionEntryLine>(e =>
            {
                e.HasIndex(x => new { x.ProductionEntryId, x.LineNo }).IsUnique();
                e.Property(x => x.ItemNo).HasMaxLength(64);
                e.Property(x => x.UnitOfMeasure).HasMaxLength(16);
            });

            modelBuilder.Entity<Item>(e =>
            {
                e.HasIndex(x => x.ItemNo).IsUnique();
                e.Property(x => x.ItemNo).HasMaxLength(64);
                e.Property(x => x.UnitOfMeasure).HasMaxLength(16);
            });




        }

    }
}
