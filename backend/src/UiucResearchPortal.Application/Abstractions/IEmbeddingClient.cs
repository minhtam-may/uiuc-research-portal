namespace UiucResearchPortal.Application.Abstractions;

/// <summary>Produces a vector embedding for a piece of text (used for both ingestion and query-time retrieval).</summary>
public interface IEmbeddingClient
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}
