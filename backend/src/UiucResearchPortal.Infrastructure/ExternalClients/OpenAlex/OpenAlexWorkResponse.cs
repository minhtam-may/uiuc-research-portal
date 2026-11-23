using System.Text.Json.Serialization;

namespace UiucResearchPortal.Infrastructure.ExternalClients.OpenAlex;

internal class OpenAlexListResponse
{
    [JsonPropertyName("meta")]
    public OpenAlexMeta Meta { get; set; } = new();

    [JsonPropertyName("results")]
    public List<OpenAlexWorkResponse> Results { get; set; } = [];
}

internal class OpenAlexMeta
{
    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

internal class OpenAlexWorkResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("publication_date")]
    public string? PublicationDate { get; set; }

    [JsonPropertyName("doi")]
    public string? Doi { get; set; }

    [JsonPropertyName("abstract_inverted_index")]
    public Dictionary<string, List<int>>? AbstractInvertedIndex { get; set; }

    [JsonPropertyName("topics")]
    public List<OpenAlexTopic> Topics { get; set; } = [];

    [JsonPropertyName("authorships")]
    public List<OpenAlexAuthorshipResponse> Authorships { get; set; } = [];
}

internal class OpenAlexTopic
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

internal class OpenAlexAuthorshipResponse
{
    [JsonPropertyName("author")]
    public OpenAlexAuthorRef Author { get; set; } = new();

    [JsonPropertyName("institutions")]
    public List<OpenAlexInstitutionRef> Institutions { get; set; } = [];
}

internal class OpenAlexAuthorRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

internal class OpenAlexInstitutionRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
