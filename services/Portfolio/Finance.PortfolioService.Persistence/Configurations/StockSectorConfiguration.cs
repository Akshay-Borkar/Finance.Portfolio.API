using Finance.PortfolioService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.PortfolioService.Persistence.Configurations;

public class StockSectorConfiguration : IEntityTypeConfiguration<StockSector>
{
    public void Configure(EntityTypeBuilder<StockSector> builder)
    {
        builder.Property(p => p.StockSectorName).IsRequired().HasMaxLength(50);

        builder.HasData(
            new StockSector
            {
                Id = new Guid("9b1c2b4d-0d51-421b-ab51-8a2cd9e5f3f7"),
                StockSectorName = "IT",
                DateCreated = new DateTime(2024, 2, 10, 12, 0, 0, DateTimeKind.Utc),
                DateModified = new DateTime(2024, 2, 10, 12, 0, 0, DateTimeKind.Utc)
            },
            new StockSector
            {
                Id = new Guid("9b1c2b4d-0d51-421b-ab51-8a2cd9e5f3f8"),
                StockSectorName = "Health",
                DateCreated = new DateTime(2024, 2, 10, 12, 0, 0, DateTimeKind.Utc),
                DateModified = new DateTime(2024, 2, 10, 12, 0, 0, DateTimeKind.Utc)
            });
    }
}
