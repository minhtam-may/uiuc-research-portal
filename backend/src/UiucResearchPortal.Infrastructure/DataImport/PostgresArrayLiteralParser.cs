using System.Text;

namespace UiucResearchPortal.Infrastructure.DataImport;

/// <summary>
/// Parses a Postgres text[] literal as exported in the CSV's "keywords" column, e.g.
/// {"parallel computing","systems biology",sampling,"domain adaptation"}
/// Elements are either double-quoted (may contain escaped quotes \" and commas) or bare tokens
/// with no special characters.
/// </summary>
internal static class PostgresArrayLiteralParser
{
    public static List<string> Parse(string? literal)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(literal))
        {
            return result;
        }

        var text = literal.Trim();
        if (text.StartsWith('{') && text.EndsWith('}'))
        {
            text = text[1..^1];
        }

        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == ',')
            {
                i++;
                continue;
            }

            if (text[i] == '"')
            {
                var sb = new StringBuilder();
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\\' && i + 1 < text.Length)
                    {
                        sb.Append(text[i + 1]);
                        i += 2;
                        continue;
                    }

                    if (text[i] == '"')
                    {
                        i++;
                        break;
                    }

                    sb.Append(text[i]);
                    i++;
                }

                result.Add(sb.ToString());
            }
            else
            {
                var start = i;
                while (i < text.Length && text[i] != ',')
                {
                    i++;
                }

                var token = text[start..i].Trim();
                if (token.Length > 0)
                {
                    result.Add(token);
                }
            }
        }

        return result;
    }
}
