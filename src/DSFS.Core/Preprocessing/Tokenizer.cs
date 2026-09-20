using System.Text.RegularExpressions;

namespace DSFS.Core.Preprocessing;

/// <summary>
/// Preprocessing step 1 (section 3.3.2.1): splits an unstructured text document into tokens/words.
/// </summary>
public static class Tokenizer
{
    private static readonly Regex WordPattern = new(@"[A-Za-z]+(?:'[A-Za-z]+)?", RegexOptions.Compiled);

    /// <summary>
    /// Splits raw text into lower-cased word tokens, discarding punctuation, digits and whitespace.
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var tokens = new List<string>();
        foreach (Match match in WordPattern.Matches(text))
        {
            var token = match.Value.ToLowerInvariant();
            if (token.Length > 1) // discard single letters left over from possessives, etc.
                tokens.Add(token);
        }
        return tokens;
    }
}
