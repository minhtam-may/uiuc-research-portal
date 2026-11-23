using UiucResearchPortal.Application.Dtos;

namespace UiucResearchPortal.Application.Abstractions;

/// <summary>Read-only queries backing the portal page (focus areas, papers, faculty).</summary>
public interface IPortalQueryService
{
    Task<IReadOnlyList<FocusAreaDto>> GetTopFocusAreasAsync(int limit, CancellationToken ct);

    Task<IReadOnlyList<PaperDto>> GetRecentPapersAsync(int limit, CancellationToken ct);

    Task<IReadOnlyList<ResearcherDto>> SearchResearchersAsync(string? search, int limit, CancellationToken ct);
}
