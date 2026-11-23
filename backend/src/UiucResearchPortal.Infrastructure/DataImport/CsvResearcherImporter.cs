using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Infrastructure.DataImport;

public class CsvResearcherImporter : ICsvResearcherImporter
{
    public IEnumerable<CsvResearcherRow> ReadRows(string csvFilePath)
    {
        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        });

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var email = csv.GetField("email")?.Trim();
            var name = csv.GetField("name")?.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var photoUrl = NullIfLiteralNull(csv.GetField("photo_url"));
            var position = NullIfLiteralNull(csv.GetField("position"));
            var keywords = PostgresArrayLiteralParser.Parse(csv.GetField("keywords"));

            yield return new CsvResearcherRow(email, name, photoUrl, position, keywords);
        }
    }

    private static string? NullIfLiteralNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Trim();
    }
}
