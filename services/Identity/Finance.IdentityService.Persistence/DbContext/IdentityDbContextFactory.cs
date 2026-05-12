using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.IdentityService.Persistence.DbContext;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=IdentityDB;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True");
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
