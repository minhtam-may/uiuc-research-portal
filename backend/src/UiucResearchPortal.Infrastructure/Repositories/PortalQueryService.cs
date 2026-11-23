using Microsoft.EntityFrameworkCore;
using UiucResearchPortal.Application.Abstractions;
using UiucResearchPortal.Application.Dtos;
using UiucResearchPortal.Infrastructure.Data;

namespace UiucResearchPortal.Infrastructure.Repositories;

public class PortalQueryService : IPortalQueryService
{
    private readonly AppDbContext _db;

    public PortalQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FocusAreaDto>> GetTopFocusAreasAsync(int limit, CancellationToken ct)
    {
        // Keywords are stored per-researcher as a flat list; aggregate in-memory since the
        // dataset is small (~1,000 researchers) and Postgres array unnesting via LINQ is awkward.
        var allKeywords = await _db.Researchers
            .AsNoTracking()
            .Select(r => r.Keywords)
            .ToListAsync(ct);

        return allKeywords
            .SelectMany(k => k)
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FocusAreaDto(g.Key, g.Count()))
            .OrderByDescending(f => f.ResearcherCount)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<PaperDto>> GetRecentPapersAsync(int limit, CancellationToken ct)
    {
        var papers = await _db.Papers
            .AsNoTracking()
            .Include(p => p.PaperAuthors)
            .ThenInclude(pa => pa.Researcher)
            .OrderByDescending(p => p.PublicationDate)
            .Take(limit)
            .ToListAsync(ct);

        return papers.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ResearcherDto>> SearchResearchersAsync(string? search, int limit, CancellationToken ct)
    {
        var query = _db.Researchers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, $"%{term}%") ||
                r.Keywords.Any(k => EF.Functions.ILike(k, $"%{term}%")));
        }

        var researchers = await query
            .OrderBy(r => r.Name)
            .Take(limit)
            .ToListAsync(ct);

        return researchers
            .Select(r => new ResearcherDto(r.Id, r.Email, r.Name, r.PhotoUrl, r.Position, r.Keywords))
            .ToList();
    }

    internal static PaperDto ToDto(Domain.Entities.Paper p) => new(
        p.Id,
        p.OpenAlexId,
        p.Title,
        p.PublicationDate,
        p.Doi,
        p.Topics,
        p.PaperAuthors.Count > 0
            ? p.PaperAuthors.Select(pa => new PaperAuthorSummaryDto(pa.ResearcherId, pa.Researcher.Name)).ToList()
            : p.AuthorDisplayNames.Select(name => new PaperAuthorSummaryDto(null, name)).ToList());
}
