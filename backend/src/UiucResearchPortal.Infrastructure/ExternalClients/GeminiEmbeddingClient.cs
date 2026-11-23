using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Infrastructure.ExternalClients;

/// <summary>Embedding client for Google Gemini's free-tier text-embedding-004 model.</summary>
public class GeminiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiEmbeddingOptions _options;

    public GeminiEmbeddingClient(HttpClient httpClient, IOptions<GeminiEmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl}/models/{_options.Model}:embedContent?key={_options.ApiKey}";

        var request = new EmbedContentRequest
        {
            Model = $"models/{_options.Model}",
            Content = new ContentPart { Parts = [new TextPart { Text = text }] }
        };

        using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedContentResponse>(cancellationToken: ct);
        return result?.Embedding.Values ?? [];
    }

    private class EmbedContentRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public ContentPart Content { get; set; } = new();
    }

    private class ContentPart
    {
        [JsonPropertyName("parts")]
        public List<TextPart> Parts { get; set; } = [];
    }

    private class TextPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class EmbedContentResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingValues Embedding { get; set; } = new();
    }

    private class EmbeddingValues
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; } = [];
    }
}
