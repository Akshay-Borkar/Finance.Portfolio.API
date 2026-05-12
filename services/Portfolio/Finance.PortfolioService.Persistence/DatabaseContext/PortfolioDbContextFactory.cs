using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.PortfolioService.Persistence.DatabaseContext;

public class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PortfolioDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=PortfolioDB;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True");
        return new PortfolioDbContext(optionsBuilder.Options);
    }
}
