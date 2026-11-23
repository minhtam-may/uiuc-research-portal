using System.Globalization;
using System.Text;

namespace UiucResearchPortal.Infrastructure.DataImport;

/// <summary>
/// Best-effort matcher between an OpenAlex authorship display name (e.g. "Jiawei Han") and a
/// known UIUC researcher's name from the CSV dataset. OpenAlex has no UIUC email to join on, so
/// this falls back to normalized exact match, then "last name + first initial" match to catch
/// minor formatting differences (middle names, diacritics, punctuation).
/// </summary>
public static class AuthorNameMatcher
{
    public static Dictionary<string, List<T>> BuildIndex<T>(IEnumerable<T> researchers, Func<T, string> nameSelector)
    {
        var index = new Dictionary<string, List<T>>();
        foreach (var researcher in researchers)
        {
            foreach (var key in Keys(nameSelector(researcher)))
            {
                if (!index.TryGetValue(key, out var list))
                {
                    list = [];
                    index[key] = list;
                }

                list.Add(researcher);
            }
        }

        return index;
    }

    public static T? Match<T>(Dictionary<string, List<T>> index, string authorshipDisplayName)
    {
        foreach (var key in Keys(authorshipDisplayName))
        {
            if (index.TryGetValue(key, out var candidates) && candidates.Count == 1)
            {
                return candidates[0];
            }
        }

        return default;
    }

    /// <summary>
    /// Produces normalization keys from strongest to weakest: full normalized name first,
    /// then "first-initial + last-name" as a looser fallback.
    /// </summary>
    private static IEnumerable<string> Keys(string name)
    {
        var normalized = Normalize(name);
        if (normalized.Length == 0)
        {
            yield break;
        }

        yield return normalized;

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var firstInitial = parts[0][0];
            var lastName = parts[^1];
            yield return $"{firstInitial}.{lastName}";
        }
    }

    private static string Normalize(string name)
    {
        var formD = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetter(c) || char.IsWhiteSpace(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (sb.Length > 0 && sb[^1] != ' ')
            {
                sb.Append(' ');
            }
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
