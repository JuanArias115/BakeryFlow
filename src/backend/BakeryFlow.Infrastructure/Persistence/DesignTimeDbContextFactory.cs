using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BakeryFlow.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BakeryFlowDbContext>
{
    public BakeryFlowDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BakeryFlowDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            "Host=localhost;Port=5432;Database=bakeryflow;Username=postgres;Password=postgres";

        builder.UseNpgsql(connectionString);
        return new BakeryFlowDbContext(builder.Options);
    }
}
