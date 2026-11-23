using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using UiucResearchPortal.Application.Abstractions;
using UiucResearchPortal.Infrastructure.Data;

namespace UiucResearchPortal.Infrastructure.Repositories;

public class RagRetriever : IRagRetriever
{
    private readonly AppDbContext _db;

    public RagRetriever(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RetrievedPaper>> FindRelevantPapersAsync(
        float[] queryEmbedding, int topK, CancellationToken ct)
    {
        var queryVector = new Vector(queryEmbedding);

        var papers = await _db.Papers
            .AsNoTracking()
            .Include(p => p.PaperAuthors)
            .ThenInclude(pa => pa.Researcher)
            .Where(p => p.Embedding != null)
            .OrderBy(p => p.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .Select(p => new
            {
                Paper = p,
                Distance = p.Embedding!.CosineDistance(queryVector)
            })
            .ToListAsync(ct);

        return papers
            .Select(x => new RetrievedPaper(
                PortalQueryService.ToDto(x.Paper),
                x.Paper.AbstractText,
                1f - (float)x.Distance))
            .ToList();
    }

    public async Task<IReadOnlyList<RetrievedResearcher>> FindRelevantResearchersAsync(
        string queryText, int topK, CancellationToken ct)
    {
        // Lightweight lexical fallback: match researchers whose keywords contain query terms.
        // Good enough for the assessment's scope; swap for embedding-based matching if precision
        // on broader queries turns out to matter later (see docs/decisions.md).
        var terms = queryText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToArray();

        if (terms.Length == 0)
        {
            return [];
        }

        var researchers = await _db.Researchers
            .AsNoTracking()
            .Where(r => terms.Any(t => r.Keywords.Any(k => EF.Functions.ILike(k, $"%{t}%"))))
            .Take(topK * 3)
            .ToListAsync(ct);

        return researchers
            .Select(r =>
            {
                var matchCount = r.Keywords.Count(k => terms.Any(t => k.Contains(t, StringComparison.OrdinalIgnoreCase)));
                return new RetrievedResearcher(
                    new Application.Dtos.ResearcherDto(r.Id, r.Email, r.Name, r.PhotoUrl, r.Position, r.Keywords),
                    matchCount);
            })
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }
}
