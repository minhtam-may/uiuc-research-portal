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

    public string Model { get; set; } = "llama-3.3-70b-versatile";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

public class GeminiEmbeddingOptions
{
    public const string SectionName = "GeminiEmbedding";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "text-embedding-004";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
