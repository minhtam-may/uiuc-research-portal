namespace UiucResearchPortal.Application.Dtos;

public record ChatRequestDto(string Question);

public record ChatCitationDto(string Type, Guid Id, string Label, string? Url);

public record ChatResponseDto(string Answer, IReadOnlyList<ChatCitationDto> Citations);
