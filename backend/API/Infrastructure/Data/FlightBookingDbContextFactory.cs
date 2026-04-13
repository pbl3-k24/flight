using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace API.Infrastructure.Data;

public class FlightBookingDbContextFactory : IDesignTimeDbContextFactory<FlightBookingDbContext>
{
    public FlightBookingDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        var optionsBuilder = new DbContextOptionsBuilder<FlightBookingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FlightBookingDbContext(optionsBuilder.Options);
    }
}
