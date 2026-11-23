using UiucResearchPortal.Infrastructure.DataImport;
using Xunit;

namespace UiucResearchPortal.Tests;

public class PostgresArrayLiteralParserTests
{
    [Fact]
    public void Parse_MixOfQuotedAndBareTokens_ReturnsAllElements()
    {
        var literal = "{\"parallel computing\",\"systems biology\",sampling,\"domain adaptation\"}";

        var result = PostgresArrayLiteralParser.Parse(literal);

        Assert.Equal(["parallel computing", "systems biology", "sampling", "domain adaptation"], result);
    }

    [Fact]
    public void Parse_EmptyOrNull_ReturnsEmptyList()
    {
        Assert.Empty(PostgresArrayLiteralParser.Parse(null));
        Assert.Empty(PostgresArrayLiteralParser.Parse(""));
        Assert.Empty(PostgresArrayLiteralParser.Parse("{}"));
    }

    [Fact]
    public void Parse_QuotedElementWithEscapedQuote_UnescapesCorrectly()
    {
        var literal = "{\"a \\\"quoted\\\" term\",plain}";

        var result = PostgresArrayLiteralParser.Parse(literal);

        Assert.Equal(["a \"quoted\" term", "plain"], result);
    }

    [Fact]
    public void Parse_SingleBareToken_ReturnsOneElement()
    {
        var result = PostgresArrayLiteralParser.Parse("{robotics}");

        Assert.Equal(["robotics"], result);
    }
}
