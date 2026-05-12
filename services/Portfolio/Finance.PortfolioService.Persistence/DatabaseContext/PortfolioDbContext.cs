using Finance.PortfolioService.Domain.Common;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Finance.PortfolioService.Persistence.DatabaseContext;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<StockSector> StockSectors { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Investment> Investments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
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
