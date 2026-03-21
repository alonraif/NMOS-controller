using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NmosController.Infrastructure.Persistence;

public sealed class DesignTimeControllerDbContextFactory : IDesignTimeDbContextFactory<ControllerDbContext>
{
    public ControllerDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ControllerDbContext>();
        builder.UseNpgsql(
            "Host=localhost;Port=5432;Database=nmos_controller;Username=nmos;Password=nmos",
            options => options.MigrationsAssembly(typeof(ControllerDbContext).Assembly.FullName));

        return new ControllerDbContext(builder.Options);
    }
}
