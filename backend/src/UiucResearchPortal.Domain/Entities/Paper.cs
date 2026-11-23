namespace UiucResearchPortal.Domain.Entities;

public class Paper
{
    public Guid Id { get; set; }

    /// <summary>OpenAlex work id, e.g. "https://openalex.org/W2741809807".</summary>
    public required string OpenAlexId { get; set; }

    public required string Title { get; set; }

    public DateOnly PublicationDate { get; set; }

    public string? Doi { get; set; }

    public string? AbstractText { get; set; }

    public List<string> Topics { get; set; } = [];

    /// <summary>
    /// Display names of all authors as returned by OpenAlex (includes non-UIUC co-authors),
    /// kept for display even when an author has no matching Researcher row.
    /// </summary>
    public List<string> AuthorDisplayNames { get; set; } = [];

    /// <summary>Embedding vector over title + abstract, used for RAG retrieval (stored as pgvector in Infrastructure).</summary>
    public float[]? Embedding { get; set; }

    public ICollection<PaperAuthor> PaperAuthors { get; set; } = [];
}
