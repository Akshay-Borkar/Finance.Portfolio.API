using Finance.IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.IdentityService.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasData(
            new ApplicationUser
            {
                Id = "8e445865-a24d-4543-a6c6-9443d048cdb9",
                Email = "admin@localhost.com",
                NormalizedEmail = "ADMIN@LOCALHOST.COM",
                ConcurrencyStamp = new Guid("7bcbb6e2-da5f-45fd-8b41-3b868f8707d8").ToString(),
                FirstName = "System",
                LastName = "Admin",
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                SecurityStamp = new Guid("b83cc847-3f38-4939-bca0-2b08bc3d01cc").ToString(),
                PasswordHash = "AQAAAAIAAYagAAAAEJrMnMRjOdWS1aqBFjvPgCnqRylg62dyb7HBghWjxN90FE+x+HT6MURzGCs1o/KiNQ==",
                EmailConfirmed = true
            },
            new ApplicationUser
            {
                Id = "9e224968-33e4-4652-b7b7-8574d048cdb9",
                Email = "user@localhost.com",
                NormalizedEmail = "USER@LOCALHOST.COM",
                ConcurrencyStamp = new Guid("412687a1-78f7-4d2f-b173-d6ce121d2ec5").ToString(),
                FirstName = "System",
                LastName = "User",
                UserName = "user",
                NormalizedUserName = "USER",
                SecurityStamp = new Guid("f54495fb-30bf-4758-821d-e823851f8e18").ToString(),
                PasswordHash = "AQAAAAIAAYagAAAAEJrMnMRjOdWS1aqBFjvPgCnqRylg62dyb7HBghWjxN90FE+x+HT6MURzGCs1o/KiNQ==",
                EmailConfirmed = true
            }
        );
    }
}
