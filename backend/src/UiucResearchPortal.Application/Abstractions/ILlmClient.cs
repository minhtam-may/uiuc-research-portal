namespace UiucResearchPortal.Application.Abstractions;

/// <summary>Chat-completion call against a free-tier LLM provider (e.g. Groq).</summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}
