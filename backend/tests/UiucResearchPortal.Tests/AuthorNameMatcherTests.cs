using UiucResearchPortal.Infrastructure.DataImport;
using Xunit;

namespace UiucResearchPortal.Tests;

public class AuthorNameMatcherTests
{
    private record FakeResearcher(string Name);

    [Fact]
    public void Match_ExactName_ReturnsResearcher()
    {
        var researchers = new[] { new FakeResearcher("Jiawei Han"), new FakeResearcher("Tamer Basar") };
        var index = AuthorNameMatcher.BuildIndex(researchers, r => r.Name);

        var match = AuthorNameMatcher.Match(index, "Jiawei Han");

        Assert.Equal("Jiawei Han", match?.Name);
    }

    [Fact]
    public void Match_DifferentCasingAndPunctuation_StillMatches()
    {
        var researchers = new[] { new FakeResearcher("Tamer Başar") };
        var index = AuthorNameMatcher.BuildIndex(researchers, r => r.Name);

        var match = AuthorNameMatcher.Match(index, "tamer basar");

        Assert.Equal("Tamer Başar", match?.Name);
    }

    [Fact]
    public void Match_MiddleNameDifference_MatchesViaInitialPlusLastName()
    {
        var researchers = new[] { new FakeResearcher("Jiawei Han") };
        var index = AuthorNameMatcher.BuildIndex(researchers, r => r.Name);

        var match = AuthorNameMatcher.Match(index, "J. Q. Han");

        Assert.Equal("Jiawei Han", match?.Name);
    }

    [Fact]
    public void Match_AmbiguousLastNameInitial_ReturnsNull()
    {
        var researchers = new[] { new FakeResearcher("Jiawei Han"), new FakeResearcher("James Han") };
        var index = AuthorNameMatcher.BuildIndex(researchers, r => r.Name);

        var match = AuthorNameMatcher.Match(index, "J. Han");

        Assert.Null(match);
    }

    [Fact]
    public void Match_NoCandidate_ReturnsNull()
    {
        var researchers = new[] { new FakeResearcher("Jiawei Han") };
        var index = AuthorNameMatcher.BuildIndex(researchers, r => r.Name);

        var match = AuthorNameMatcher.Match(index, "Someone Else");

        Assert.Null(match);
    }
}
