namespace UiucResearchPortal.Infrastructure.ExternalClients.OpenAlex;

public class OpenAlexOptions
{
    public const string SectionName = "OpenAlex";

    public string BaseUrl { get; set; } = "https://api.openalex.org";

    /// <summary>UIUC's OpenAlex institution id.</summary>
    public string InstitutionId { get; set; } = "I157725225";

    /// <summary>
    /// Optional: an email address sent as the `mailto` query param, which OpenAlex's "polite pool"
    /// uses to grant more consistent response times. Not an API key — OpenAlex has none for this tier.
    /// </summary>
    public string? PoliteMailto { get; set; }

    /// <summary>Optional free API key — raises the daily request budget tenfold; works fine without one.</summary>
    public string? ApiKey { get; set; }

    public int PerPage { get; set; } = 100;
}
