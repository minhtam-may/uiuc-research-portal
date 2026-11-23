namespace UiucResearchPortal.Application.Dtos;

public record PaperAuthorSummaryDto(Guid? ResearcherId, string DisplayName);

public record PaperDto(
    Guid Id,
    string OpenAlexId,
    string Title,
    DateOnly PublicationDate,
    string? Doi,
    IReadOnlyList<string> Topics,
    IReadOnlyList<PaperAuthorSummaryDto> Authors);

public record FocusAreaDto(string Keyword, int ResearcherCount);
