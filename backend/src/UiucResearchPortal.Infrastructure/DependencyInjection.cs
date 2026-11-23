using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UiucResearchPortal.Application.Abstractions;
using UiucResearchPortal.Infrastructure.Data;
using UiucResearchPortal.Infrastructure.ExternalClients;
using UiucResearchPortal.Infrastructure.ExternalClients.OpenAlex;
using UiucResearchPortal.Infrastructure.DataImport;
using UiucResearchPortal.Infrastructure.Repositories;

namespace UiucResearchPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres configuration.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddScoped<IPortalQueryService, PortalQueryService>();
        services.AddScoped<IRagRetriever, RagRetriever>();

        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));
        services.Configure<GeminiEmbeddingOptions>(configuration.GetSection(GeminiEmbeddingOptions.SectionName));
        services.Configure<OpenAlexOptions>(configuration.GetSection(OpenAlexOptions.SectionName));

        services.AddHttpClient<ILlmClient, GroqLlmClient>();
        services.AddHttpClient<IEmbeddingClient, GeminiEmbeddingClient>();
        services.AddHttpClient<IOpenAlexClient, OpenAlexClient>();

        services.AddScoped<ICsvResearcherImporter, CsvResearcherImporter>();

        return services;
    }
}
