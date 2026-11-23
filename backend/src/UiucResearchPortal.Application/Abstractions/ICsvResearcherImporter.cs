namespace UiucResearchPortal.Application.Abstractions;

public record CsvResearcherRow(
    string Email,
    string Name,
    string? PhotoUrl,
    string? Position,
    IReadOnlyList<string> Keywords);

/// <summary>Reads the provided `illinois-researchers.csv` dataset.</summary>
public interface ICsvResearcherImporter
{
    IEnumerable<CsvResearcherRow> ReadRows(string csvFilePath);
}
