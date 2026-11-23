namespace UiucResearchPortal.Application.Abstractions;

public record OpenAlexAuthorship(string DisplayName, string? OpenAlexAuthorId, bool IsUiucAffiliated);

public record OpenAlexWork(
    string OpenAlexId,
    string Title,
    DateOnly PublicationDate,
    string? Doi,
    string? AbstractText,
    IReadOnlyList<string> Topics,
    IReadOnlyList<OpenAlexAuthorship> Authorships);

/// <summary>
/// Streams UIUC-affiliated works from the OpenAlex API, newest first, handling cursor pagination
/// and rate limiting internally.
/// </summary>
public interface IOpenAlexClient
{
    IAsyncEnumerable<OpenAlexWork> StreamUiucWorksAsync(
        DateOnly fromDate,
        int maxResults,
        CancellationToken ct);
}
