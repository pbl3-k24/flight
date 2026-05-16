namespace API.Application.Interfaces;

public interface ICsvSeedService
{
    Task<CsvSeedResult> SeedAsync(string seedDirectory, CancellationToken cancellationToken = default);
}

public class CsvSeedResult
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Messages { get; set; } = [];

    public bool HasErrors => Errors.Count > 0;
}
