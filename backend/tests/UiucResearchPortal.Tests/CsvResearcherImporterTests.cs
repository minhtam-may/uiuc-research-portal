using UiucResearchPortal.Infrastructure.DataImport;
using Xunit;

namespace UiucResearchPortal.Tests;

public class CsvResearcherImporterTests
{
    private static string CsvPath => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "data", "illinois-researchers.csv");

    [Fact]
    public void ReadRows_RealDataset_ParsesAllRowsWithKeywords()
    {
        Assert.True(File.Exists(CsvPath), $"Expected dataset at {Path.GetFullPath(CsvPath)}");

        var importer = new CsvResearcherImporter();
        var rows = importer.ReadRows(CsvPath).ToList();

        Assert.Equal(1000, rows.Count);
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.Email)));
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
        Assert.All(rows, r => Assert.NotEmpty(r.Keywords));

        var janHan = rows.Single(r => r.Email == "hanj@illinois.edu");
        Assert.Equal("Jiawei Han", janHan.Name);
        Assert.Contains("text mining", janHan.Keywords);
        Assert.Null(janHan.PhotoUrl);
    }
}
