using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UiucResearchPortal.Application;
using UiucResearchPortal.Infrastructure;
using UiucResearchPortal.Ingestion;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<IngestionMarker>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddApplication();
services.AddInfrastructure(configuration);
services.Configure<IngestionOptions>(configuration.GetSection(IngestionOptions.SectionName));
services.AddScoped<IngestionRunner>();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var csvPath = Path.Combine(AppContext.BaseDirectory, "data", "illinois-researchers.csv");
if (!File.Exists(csvPath))
{
    // Fallback for `dotnet run` from source (bin/ copy may be stale) — resolve relative to repo root.
    csvPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "illinois-researchers.csv"));
}

var runner = scope.ServiceProvider.GetRequiredService<IngestionRunner>();
await runner.RunAsync(csvPath, CancellationToken.None);

namespace UiucResearchPortal.Ingestion
{
    /// <summary>Marker type anchoring this assembly's user-secrets id (shared with the Api project).</summary>
    public class IngestionMarker;
}
