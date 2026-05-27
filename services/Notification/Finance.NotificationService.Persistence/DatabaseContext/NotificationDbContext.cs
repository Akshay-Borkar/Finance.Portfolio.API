using Finance.NotificationService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finance.NotificationService.Persistence.DatabaseContext;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<PortfolioReviewEntity> PortfolioReviews => Set<PortfolioReviewEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortfolioReviewEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.NewsSummary).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(e => e.RiskAnalysis).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(e => e.WeeklyRecommendation).HasColumnType("nvarchar(max)").IsRequired();
        });
    }
}
