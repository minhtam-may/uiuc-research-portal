using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Infrastructure.ExternalClients.OpenAlex;

public class OpenAlexClient : IOpenAlexClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAlexOptions _options;
    private readonly ILogger<OpenAlexClient> _logger;

    public OpenAlexClient(HttpClient httpClient, IOptions<OpenAlexOptions> options, ILogger<OpenAlexClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<OpenAlexWork> StreamUiucWorksAsync(
        DateOnly fromDate,
        int maxResults,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var cursor = "*";
        var fetched = 0;

        while (cursor is not null && fetched < maxResults)
        {
            var url = BuildUrl(fromDate, cursor);
            var response = await _httpClient.GetFromJsonAsync<OpenAlexListResponse>(url, ct)
                ?? throw new InvalidOperationException("OpenAlex returned an empty response.");

            foreach (var work in response.Results)
            {
                if (fetched >= maxResults)
                {
                    break;
                }

                var mapped = Map(work);
                if (mapped is not null)
                {
                    fetched++;
                    yield return mapped;
                }
            }

            cursor = response.Meta.NextCursor;
            _logger.LogInformation("OpenAlex: fetched {Fetched}/{Max} works (total matching: {Total})",
                fetched, maxResults, response.Meta.Count);
        }
    }

    private string BuildUrl(DateOnly fromDate, string cursor)
    {
        var filter = $"institutions.id:{_options.InstitutionId},from_publication_date:{fromDate:yyyy-MM-dd},type:article";
        var select = "id,title,display_name,publication_date,doi,abstract_inverted_index,topics,authorships";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["filter"] = filter;
        query["select"] = select;
        query["sort"] = "publication_date:desc";
        query["per-page"] = _options.PerPage.ToString();
        query["cursor"] = cursor;

        if (!string.IsNullOrWhiteSpace(_options.PoliteMailto))
        {
            query["mailto"] = _options.PoliteMailto;
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            query["api_key"] = _options.ApiKey;
        }

        return $"{_options.BaseUrl}/works?{query}";
    }

    private OpenAlexWork? Map(OpenAlexWorkResponse work)
    {
        var title = work.Title ?? work.DisplayName;
        if (string.IsNullOrWhiteSpace(title) || !DateOnly.TryParse(work.PublicationDate, out var publicationDate))
        {
            return null;
        }

        var authorships = work.Authorships
            .Select(a => new OpenAlexAuthorship(
                a.Author.DisplayName,
                a.Author.Id,
                a.Institutions.Any(i => i.Id?.Contains(_options.InstitutionId, StringComparison.OrdinalIgnoreCase) == true)))
            .ToList();

        return new OpenAlexWork(
            work.Id,
            title,
            publicationDate,
            work.Doi,
            AbstractInvertedIndexDecoder.Decode(work.AbstractInvertedIndex),
            work.Topics.Select(t => t.DisplayName).ToList(),
            authorships);
    }
}
