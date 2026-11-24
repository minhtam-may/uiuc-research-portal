using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UiucResearchPortal.Application.Abstractions;
using UiucResearchPortal.Domain.Entities;
using UiucResearchPortal.Infrastructure.Data;
using UiucResearchPortal.Infrastructure.DataImport;

namespace UiucResearchPortal.Ingestion;

/// <summary>
/// Orchestrates the one-shot seeding pipeline:
///   1. Import researchers from the provided CSV dataset.
///   2. Fetch recent UIUC works from OpenAlex, matching authors to known researchers by name.
///   3. Compute an embedding per paper (title + abstract) for RAG retrieval.
///   4. Persist everything to Postgres.
/// Safe to re-run: upserts by natural key (email / OpenAlex id) rather than inserting duplicates.
/// </summary>
public class IngestionRunner
{
    private static readonly DateOnly TwoYearsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2));

    private readonly AppDbContext _db;
    private readonly ICsvResearcherImporter _csvImporter;
    private readonly IOpenAlexClient _openAlexClient;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IngestionOptions _options;
    private readonly ILogger<IngestionRunner> _logger;

    public IngestionRunner(
        AppDbContext db,
        ICsvResearcherImporter csvImporter,
        IOpenAlexClient openAlexClient,
        IEmbeddingClient embeddingClient,
        Microsoft.Extensions.Options.IOptions<IngestionOptions> options,
        ILogger<IngestionRunner> logger)
    {
        _db = db;
        _csvImporter = csvImporter;
        _openAlexClient = openAlexClient;
        _embeddingClient = embeddingClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(string csvFilePath, CancellationToken ct)
    {
        await _db.Database.MigrateAsync(ct);

        var researchers = await ImportResearchersAsync(csvFilePath, ct);
        _logger.LogInformation("Researchers in DB: {Count}", researchers.Count);

        var nameIndex = AuthorNameMatcher.BuildIndex(researchers, r => r.Name);

        await ImportPapersAsync(nameIndex, ct);
    }

    private async Task<List<Researcher>> ImportResearchersAsync(string csvFilePath, CancellationToken ct)
    {
        var existingByEmail = await _db.Researchers.ToDictionaryAsync(r => r.Email, StringComparer.OrdinalIgnoreCase, ct);
        var imported = new List<Researcher>();

        foreach (var row in _csvImporter.ReadRows(csvFilePath))
        {
            if (existingByEmail.TryGetValue(row.Email, out var existing))
            {
                // The provided dataset has at least one duplicate email (same person listed
                // twice); treat a repeat within this same run as an update to the row already
                // seen, not a new insert — otherwise this throws a unique-constraint violation.
                existing.Name = row.Name;
                existing.PhotoUrl = row.PhotoUrl;
                existing.Position = row.Position;
                existing.Keywords = row.Keywords.ToList();
                imported.Add(existing);
            }
            else
            {
                var researcher = new Researcher
                {
                    Id = Guid.NewGuid(),
                    Email = row.Email,
                    Name = row.Name,
                    PhotoUrl = row.PhotoUrl,
                    Position = row.Position,
                    Keywords = row.Keywords.ToList()
                };
                _db.Researchers.Add(researcher);
                existingByEmail[row.Email] = researcher;
                imported.Add(researcher);
            }
        }

        await _db.SaveChangesAsync(ct);
        return imported;
    }

    /// <summary>Small delay between embedding calls to stay under free-tier per-minute quotas proactively,
    /// in addition to the reactive retry-with-backoff inside GeminiEmbeddingClient.</summary>
    private static readonly TimeSpan EmbeddingThrottle = TimeSpan.FromMilliseconds(250);

    private async Task ImportPapersAsync(Dictionary<string, List<Researcher>> researcherNameIndex, CancellationToken ct)
    {
        var existingOpenAlexIds = await _db.Papers.Select(p => p.OpenAlexId).ToListAsync(ct);
        var existingSet = new HashSet<string>(existingOpenAlexIds, StringComparer.OrdinalIgnoreCase);

        var processed = 0;
        var newPapers = 0;

        await foreach (var work in _openAlexClient.StreamUiucWorksAsync(TwoYearsAgo, _options.MaxWorksToFetch, ct))
        {
            processed++;

            if (existingSet.Contains(work.OpenAlexId))
            {
                continue;
            }

            var uiucAuthors = work.Authorships.Where(a => a.IsUiucAffiliated).ToList();
            if (uiucAuthors.Count == 0)
            {
                // OpenAlex's institution filter can occasionally include works via a secondary
                // affiliation that doesn't survive our authorship-level check; skip rather than
                // mis-attribute a paper to no one.
                continue;
            }

            var embeddingText = $"{work.Title}\n\n{work.AbstractText}".Trim();
            float[]? embedding = null;
            try
            {
                embedding = await _embeddingClient.EmbedAsync(embeddingText, ct);
                await Task.Delay(EmbeddingThrottle, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to embed paper {OpenAlexId}; storing without embedding.", work.OpenAlexId);
            }

            var paper = new Paper
            {
                Id = Guid.NewGuid(),
                OpenAlexId = work.OpenAlexId,
                Title = work.Title,
                PublicationDate = work.PublicationDate,
                Doi = work.Doi,
                AbstractText = work.AbstractText,
                Topics = work.Topics.ToList(),
                AuthorDisplayNames = work.Authorships.Select(a => a.DisplayName).ToList(),
                Embedding = embedding
            };

            foreach (var authorship in uiucAuthors)
            {
                var matchedResearcher = AuthorNameMatcher.Match(researcherNameIndex, authorship.DisplayName);
                if (matchedResearcher is not null)
                {
                    paper.PaperAuthors.Add(new PaperAuthor { PaperId = paper.Id, ResearcherId = matchedResearcher.Id });
                }
            }

            _db.Papers.Add(paper);
            newPapers++;

            if (newPapers % 50 == 0)
            {
                await _db.SaveChangesAsync(ct);
                // Detach everything saved so far: without this, the change tracker keeps every
                // Paper/PaperAuthor from the whole run (up to ~12,000), and per-entity
                // change-detection cost grows accordingly as the run progresses.
                _db.ChangeTracker.Clear();
                _logger.LogInformation("Ingested {New} new papers so far ({Processed} processed)...", newPapers, processed);
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Done. Processed {Processed} works from OpenAlex, added {New} new papers.", processed, newPapers);
    }
}
