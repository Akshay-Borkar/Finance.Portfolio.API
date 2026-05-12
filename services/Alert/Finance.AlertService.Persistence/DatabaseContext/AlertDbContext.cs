using Finance.AlertService.Domain.Common;
using Finance.AlertService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finance.AlertService.Persistence.DatabaseContext;

public class AlertDbContext : DbContext
{
    public AlertDbContext(DbContextOptions<AlertDbContext> options) : base(options) { }

    public DbSet<StockPriceAlert> StockPriceAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockPriceAlert>(b =>
        {
            b.Property(p => p.Ticker).IsRequired().HasMaxLength(10);
            b.Property(p => p.UserEmail).IsRequired().HasMaxLength(256);
            b.Property(p => p.TargetPrice).HasColumnType("decimal(18,4)");
        });

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
        {
            entry.Entity.DateModified = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
                entry.Entity.DateCreated = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
