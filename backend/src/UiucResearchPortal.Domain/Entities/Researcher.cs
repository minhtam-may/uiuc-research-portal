namespace UiucResearchPortal.Domain.Entities;

public class Researcher
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string Name { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Position { get; set; }

    /// <summary>
    /// Research keywords as provided in the source dataset (flattened from a Postgres text[] literal).
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    /// <summary>
    /// OpenAlex author id, populated once this researcher is matched to an OpenAlex author
    /// (best-effort match by display name + UIUC institution affiliation).
    /// </summary>
    public string? OpenAlexAuthorId { get; set; }

    public ICollection<PaperAuthor> PaperAuthors { get; set; } = [];
}
