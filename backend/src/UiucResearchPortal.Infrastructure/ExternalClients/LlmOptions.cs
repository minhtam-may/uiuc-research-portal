namespace UiucResearchPortal.Infrastructure.ExternalClients;

/// <summary>
/// Settings for the free-tier LLM provider (Groq) and embedding provider (Gemini).
/// Bind from configuration section "Llm" / "Embedding"; API keys come from env vars or user-secrets,
/// never committed to source control.
/// </summary>
public class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Groq's free-tier model lineup changes over time; llama-3.3-70b-versatile was retired.
    /// gpt-oss-120b is the current general-purpose chat model available on the free tier.
    /// </summary>
    public string Model { get; set; } = "openai/gpt-oss-120b";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

public class GeminiEmbeddingOptions
{
    public const string SectionName = "GeminiEmbedding";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>text-embedding-004 was retired; gemini-embedding-001 is the current free-tier model.</summary>
    public string Model { get; set; } = "gemini-embedding-001";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>Must match PaperConfiguration.EmbeddingDimensions (768) — the pgvector column width.</summary>
    public int OutputDimensionality { get; set; } = 768;
}
