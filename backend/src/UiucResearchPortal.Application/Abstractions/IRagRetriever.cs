using UiucResearchPortal.Application.Dtos;

namespace UiucResearchPortal.Application.Abstractions;

public record RetrievedPaper(PaperDto Paper, string? AbstractText, float Score);

public record RetrievedResearcher(ResearcherDto Researcher, float Score);

/// <summary>Vector-similarity retrieval over papers and researchers, used to build the RAG context.</summary>
public interface IRagRetriever
{
    Task<IReadOnlyList<RetrievedPaper>> FindRelevantPapersAsync(float[] queryEmbedding, int topK, CancellationToken ct);

    Task<IReadOnlyList<RetrievedResearcher>> FindRelevantResearchersAsync(string queryText, int topK, CancellationToken ct);
}
