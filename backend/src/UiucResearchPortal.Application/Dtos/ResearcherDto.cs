namespace UiucResearchPortal.Application.Dtos;

public record ResearcherDto(
    Guid Id,
    string Email,
    string Name,
    string? PhotoUrl,
    string? Position,
    IReadOnlyList<string> Keywords);
