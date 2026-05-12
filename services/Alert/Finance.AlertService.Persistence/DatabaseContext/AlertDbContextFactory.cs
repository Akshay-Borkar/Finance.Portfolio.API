using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.AlertService.Persistence.DatabaseContext;

public class AlertDbContextFactory : IDesignTimeDbContextFactory<AlertDbContext>
{
    public AlertDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AlertDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=AlertDB;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True");
        return new AlertDbContext(optionsBuilder.Options);
    }
}
