namespace UiucResearchPortal.Infrastructure.ExternalClients.OpenAlex;

/// <summary>
/// OpenAlex returns abstracts as an "inverted index": a map of word -> [positions], to save space
/// and to sidestep copyright concerns around redistributing raw abstract text. This reconstructs
/// the plain-text abstract by placing each word back at its recorded position(s).
/// </summary>
internal static class AbstractInvertedIndexDecoder
{
    public static string? Decode(Dictionary<string, List<int>>? invertedIndex)
    {
        if (invertedIndex is null || invertedIndex.Count == 0)
        {
            return null;
        }

        var maxPosition = invertedIndex.Values.SelectMany(positions => positions).DefaultIfEmpty(-1).Max();
        if (maxPosition < 0)
        {
            return null;
        }

        var words = new string?[maxPosition + 1];
        foreach (var (word, positions) in invertedIndex)
        {
            foreach (var position in positions)
            {
                if (position >= 0 && position < words.Length)
                {
                    words[position] = word;
                }
            }
        }

        return string.Join(' ', words.Where(w => w is not null));
    }
}
