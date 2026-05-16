namespace API.Extensions;

using API.Application.Interfaces;

public static class CsvSeedExtensions
{
    public static async Task SeedCsvDataAsync(this WebApplication app)
    {
        var enabled = app.Configuration.GetValue("CsvSeed:Enabled", false);
        if (!enabled)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CsvSeed");
        var csvSeedService = scope.ServiceProvider.GetRequiredService<ICsvSeedService>();

        var configuredDirectory = app.Configuration["CsvSeed:Directory"] ?? "seed-data";
        var seedDirectory = Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.Combine(app.Environment.ContentRootPath, configuredDirectory);

        var result = await csvSeedService.SeedAsync(seedDirectory);
        if (result.HasErrors)
        {
            throw new InvalidOperationException(
                "CSV seed failed:" + Environment.NewLine + string.Join(Environment.NewLine, result.Errors));
        }

        logger.LogInformation(
            "CSV seed completed. Inserted={Inserted}, Skipped={Skipped}, Messages={MessageCount}",
            result.Inserted,
            result.Skipped,
            result.Messages.Count);
    }
}
