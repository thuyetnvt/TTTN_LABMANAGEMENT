using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace LabManagementAPI.Data;

/// <summary>
/// Keeps EF migrations independent from runtime secrets and the running Docker stack.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=lab_management;Uid=lab_app;Pwd=AppPassword123!;",
            new MySqlServerVersion(new Version(8, 4, 0)));
        return new AppDbContext(optionsBuilder.Options);
    }
}
