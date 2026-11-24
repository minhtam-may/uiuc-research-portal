using System.Text;
using UiucResearchPortal.Application.Abstractions;
using UiucResearchPortal.Application.Dtos;

namespace UiucResearchPortal.Application.Services;

/// <summary>
/// Orchestrates the RAG flow for the domain-specific assistant: embed the question,
/// retrieve relevant papers/researchers, build a grounded prompt, and call the LLM.
/// </summary>
public class ResearchChatService
{
    private const int TopKPapers = 8;
    private const int TopKResearchers = 5;

    private readonly IEmbeddingClient _embeddingClient;
    private readonly IRagRetriever _retriever;
    private readonly ILlmClient _llmClient;

    public ResearchChatService(IEmbeddingClient embeddingClient, IRagRetriever retriever, ILlmClient llmClient)
    {
        _embeddingClient = embeddingClient;
        _retriever = retriever;
        _llmClient = llmClient;
    }

    public async Task<ChatResponseDto> AnswerAsync(string question, CancellationToken ct)
    {
        var queryEmbedding = await _embeddingClient.EmbedAsync(question, ct);

        // Sequential, not Task.WhenAll: both calls go through the same scoped IRagRetriever /
        // AppDbContext, and EF Core's DbContext is not safe for concurrent operations on the
        // same instance (see docs/decisions.md).
        var papers = await _retriever.FindRelevantPapersAsync(queryEmbedding, TopKPapers, ct);
        var researchers = await _retriever.FindRelevantResearchersAsync(question, TopKResearchers, ct);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(question, papers, researchers);

        var answer = await _llmClient.CompleteAsync(systemPrompt, userPrompt, ct);

        var citations = new List<ChatCitationDto>();
        citations.AddRange(papers.Select(p => new ChatCitationDto(
            "paper", p.Paper.Id, p.Paper.Title, p.Paper.Doi)));
        citations.AddRange(researchers.Select(r => new ChatCitationDto(
            "researcher", r.Researcher.Id, r.Researcher.Name, null)));

        return new ChatResponseDto(answer, citations);
    }

    private static string BuildSystemPrompt() =>
        "You are a research assistant for the University of Illinois Urbana-Champaign (UIUC). " +
        "Answer ONLY using the provided context about UIUC papers and faculty. " +
        "If the context does not contain enough information to answer, say so clearly instead of guessing. " +
        "Keep answers concise and mention the specific papers or faculty members that support your answer.";

    private static string BuildUserPrompt(
        string question,
        IReadOnlyList<RetrievedPaper> papers,
        IReadOnlyList<RetrievedResearcher> researchers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Relevant UIUC papers");
        foreach (var p in papers)
        {
            sb.AppendLine($"- \"{p.Paper.Title}\" ({p.Paper.PublicationDate:yyyy-MM-dd}) " +
                          $"by {string.Join(", ", p.Paper.Authors.Select(a => a.DisplayName))}");
        }

        sb.AppendLine();
        sb.AppendLine("## Relevant UIUC faculty");
        foreach (var r in researchers)
        {
            sb.AppendLine($"- {r.Researcher.Name} ({r.Researcher.Position}): " +
                          $"{string.Join(", ", r.Researcher.Keywords.Take(8))}");
        }

        sb.AppendLine();
        sb.AppendLine("## Question");
        sb.AppendLine(question);

        return sb.ToString();
    }
}
