using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using UiucResearchPortal.Application.Abstractions;
using UiucResearchPortal.Domain.Entities;
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

        // Build "Keywords.Any(k => ILIKE(k, '%term%'))" per term and OR them together, rather than
        // nesting terms.Any(...) around a DB-side Any(...): mixing a client-side array with a
        // DB column inside a single nested LINQ Any is not guaranteed to translate with Npgsql.
        // This keeps each term's predicate DB-translatable and just composes them with OrElse.
        Expression<Func<Researcher, bool>>? predicate = null;
        foreach (var term in terms)
        {
            var pattern = $"%{term}%";
            Expression<Func<Researcher, bool>> termPredicate =
                r => r.Keywords.Any(k => EF.Functions.ILike(k, pattern));

            predicate = predicate is null
                ? termPredicate
                : Combine(predicate, termPredicate);
        }

        var researchers = await _db.Researchers
            .AsNoTracking()
            .Where(predicate!)
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

    private static Expression<Func<Researcher, bool>> Combine(
        Expression<Func<Researcher, bool>> left, Expression<Func<Researcher, bool>> right)
    {
        var parameter = left.Parameters[0];
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body);
        var combinedBody = Expression.OrElse(left.Body, rightBody!);
        return Expression.Lambda<Func<Researcher, bool>>(combinedBody, parameter);
    }

    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }
}
