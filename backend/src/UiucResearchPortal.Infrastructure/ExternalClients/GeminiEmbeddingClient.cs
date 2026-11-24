using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Infrastructure.ExternalClients;

/// <summary>Embedding client for Google Gemini's free-tier text-embedding-004 model.</summary>
public class GeminiEmbeddingClient : IEmbeddingClient
{
    private const int MaxRetries = 5;

    private readonly HttpClient _httpClient;
    private readonly GeminiEmbeddingOptions _options;
    private readonly ILogger<GeminiEmbeddingClient> _logger;

    public GeminiEmbeddingClient(HttpClient httpClient, IOptions<GeminiEmbeddingOptions> options, ILogger<GeminiEmbeddingClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl}/models/{_options.Model}:embedContent?key={_options.ApiKey}";

        var request = new EmbedContentRequest
        {
            Model = $"models/{_options.Model}",
            Content = new ContentPart { Parts = [new TextPart { Text = text }] },
            OutputDimensionality = _options.OutputDimensionality
        };

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            using var response = await _httpClient.PostAsJsonAsync(url, request, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetries)
            {
                // Free-tier embedding quotas are rate-limited per minute. Back off and retry
                // rather than silently dropping the embedding (see docs/decisions.md) — the
                // caller (ingestion) makes thousands of these calls sequentially.
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    "Gemini embedding rate-limited (attempt {Attempt}/{Max}); waiting {Delay}s before retry.",
                    attempt, MaxRetries, retryAfter.TotalSeconds);
                await Task.Delay(retryAfter, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbedContentResponse>(cancellationToken: ct);
            return result?.Embedding.Values ?? [];
        }

        throw new HttpRequestException($"Gemini embedding API still rate-limited after {MaxRetries} attempts.");
    }

    private class EmbedContentRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public ContentPart Content { get; set; } = new();

        [JsonPropertyName("outputDimensionality")]
        public int OutputDimensionality { get; set; }
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
