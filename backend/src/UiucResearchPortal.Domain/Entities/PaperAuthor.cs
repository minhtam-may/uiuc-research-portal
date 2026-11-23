namespace UiucResearchPortal.Domain.Entities;

/// <summary>Join entity linking a Paper to a known UIUC Researcher (many-to-many).</summary>
public class PaperAuthor
{
    public Guid PaperId { get; set; }

    public Paper Paper { get; set; } = null!;

    public Guid ResearcherId { get; set; }

    public Researcher Researcher { get; set; } = null!;
}
