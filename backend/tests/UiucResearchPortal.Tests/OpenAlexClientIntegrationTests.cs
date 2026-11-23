using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UiucResearchPortal.Infrastructure.ExternalClients.OpenAlex;
using Xunit;

namespace UiucResearchPortal.Tests;

/// <summary>
/// Hits the real OpenAlex API (no key required). Skipped by default in CI-like environments
/// without network access — run explicitly with `dotnet test --filter Category=Integration`
/// when you want to verify against the live API (e.g. after changing filter syntax).
/// </summary>
[Trait("Category", "Integration")]
public class OpenAlexClientIntegrationTests
{
    [Fact]
    public async Task StreamUiucWorksAsync_RealApi_ReturnsUiucAffiliatedRecentArticles()
    {
        var options = Options.Create(new OpenAlexOptions());
        using var httpClient = new HttpClient();
        var client = new OpenAlexClient(httpClient, options, NullLogger<OpenAlexClient>.Instance);

        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2));
        var works = new List<UiucResearchPortal.Application.Abstractions.OpenAlexWork>();

        await foreach (var work in client.StreamUiucWorksAsync(fromDate, maxResults: 5, CancellationToken.None))
        {
            works.Add(work);
        }

        Assert.NotEmpty(works);
        Assert.All(works, w => Assert.False(string.IsNullOrWhiteSpace(w.Title)));
        Assert.All(works, w => Assert.True(w.PublicationDate >= fromDate));
        Assert.All(works, w => Assert.Contains(w.Authorships, a => a.IsUiucAffiliated));
    }
}
