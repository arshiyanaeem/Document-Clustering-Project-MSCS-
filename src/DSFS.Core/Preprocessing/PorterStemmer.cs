using System.Text;

namespace DSFS.Core.Preprocessing;

/// <summary>
/// Preprocessing step 3 (section 3.3.2.3): reduces words to their root/stem form using
/// Porter's stemming algorithm (Porter, 1980), exactly as referenced in the thesis.
///
/// This is a from-scratch C# implementation of the classic five-step suffix-stripping
/// algorithm (steps 1a, 1b, 1c, 2, 3, 4, 5a, 5b) driven by the standard consonant/vowel
/// "measure" (m) of a word's CVCV...C/V pattern.
/// </summary>
public class PorterStemmer
{
    private const string Vowels = "aeiou";

    public string Stem(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length <= 2)
            return word;

        var w = word.ToLowerInvariant();

        w = Step1A(w);
        w = Step1B(w);
        w = Step1C(w);
        w = Step2(w);
        w = Step3(w);
        w = Step4(w);
        w = Step5A(w);
        w = Step5B(w);

        return w;
    }

    public List<string> StemAll(IEnumerable<string> words) => words.Select(Stem).ToList();

    // --- Helpers -----------------------------------------------------------

    // Computes the "measure" m of the stem: the number of VC sequences.
    private static int Measure(string stem)
    {
        var pattern = new StringBuilder();
        for (int i = 0; i < stem.Length; i++)
            pattern.Append(IsConsonant(stem, i) ? 'C' : 'V');

        string p = pattern.ToString();
        // collapse repeats: CCVVCC -> CVC pattern of transitions
        int m = 0;
        for (int i = 0; i < p.Length - 1; i++)
        {
            if (p[i] == 'V' && p[i + 1] == 'C')
                m++;
        }
        return m;
    }

    private static bool IsConsonant(string w, int i)
    {
        char c = w[i];
        if (Vowels.IndexOf(c) >= 0) return false;
        if (c != 'y') return true;
        // y is a consonant if it is at position 0, or the previous letter is a consonant.
        return i == 0 || IsConsonant(w, i - 1);
    }

    private static bool ContainsVowel(string stem)
    {
        for (int i = 0; i < stem.Length; i++)
            if (!IsConsonant(stem, i)) return true;
        return false;
    }

    private static bool EndsWithDoubleConsonant(string w)
    {
        if (w.Length < 2) return false;
        int last = w.Length - 1;
        return w[last] == w[last - 1] && IsConsonant(w, last) && IsConsonant(w, last - 1);
    }

    /// <summary>Cvc rule: stem ends consonant-vowel-consonant, and the final consonant is not w, x or y.</summary>
    private static bool EndsCvc(string w)
    {
        if (w.Length < 3) return false;
        int n = w.Length - 1;
        bool c1 = IsConsonant(w, n - 2);
        bool v = !IsConsonant(w, n - 1);
        bool c2 = IsConsonant(w, n);
        char lastChar = w[n];
        return c1 && v && c2 && lastChar != 'w' && lastChar != 'x' && lastChar != 'y';
    }

    private static bool EndsWith(string w, string suffix) => w.EndsWith(suffix, StringComparison.Ordinal);

    private static bool ReplaceIfMeasureAtLeast(ref string w, string suffix, string replacement, int minMeasure)
    {
        if (!EndsWith(w, suffix)) return false;
        string stem = w[..^suffix.Length];
        if (Measure(stem) >= minMeasure)
        {
            w = stem + replacement;
            return true;
        }
        return false;
    }

    // --- Steps ---------------------------------------------------------------

    private static string Step1A(string w)
    {
        if (EndsWith(w, "sses")) return w[..^4] + "ss";
        if (EndsWith(w, "ies")) return w[..^3] + "i";
        if (EndsWith(w, "ss")) return w;
        if (EndsWith(w, "s") && w.Length > 1) return w[..^1];
        return w;
    }

    private static string Step1B(string w)
    {
        bool applied = false;
        if (EndsWith(w, "eed"))
        {
            string stem = w[..^3];
            if (Measure(stem) > 0) w = stem + "ee";
            return w;
        }
        if (EndsWith(w, "ed"))
        {
            string stem = w[..^2];
            if (ContainsVowel(stem)) { w = stem; applied = true; }
        }
        else if (EndsWith(w, "ing"))
        {
            string stem = w[..^3];
            if (ContainsVowel(stem)) { w = stem; applied = true; }
        }

        if (applied)
        {
            if (EndsWith(w, "at") || EndsWith(w, "bl") || EndsWith(w, "iz"))
                w += "e";
            else if (EndsWithDoubleConsonant(w) && !w.EndsWith("l") && !w.EndsWith("s") && !w.EndsWith("z"))
                w = w[..^1];
            else if (Measure(w) == 1 && EndsCvc(w))
                w += "e";
        }
        return w;
    }

    private static string Step1C(string w)
    {
        if (EndsWith(w, "y") && w.Length > 1 && ContainsVowel(w[..^1]))
            w = w[..^1] + "i";
        return w;
    }

    private static string Step2(string w)
    {
        (string, string)[] map =
        {
            ("ational","ate"), ("tional","tion"), ("enci","ence"), ("anci","ance"), ("izer","ize"),
            ("abli","able"), ("alli","al"), ("entli","ent"), ("eli","e"), ("ousli","ous"),
            ("ization","ize"), ("ation","ate"), ("ator","ate"), ("alism","al"), ("iveness","ive"),
            ("fulness","ful"), ("ousness","ous"), ("aliti","al"), ("iviti","ive"), ("biliti","ble")
        };
        foreach (var (suffix, replacement) in map)
            if (ReplaceIfMeasureAtLeast(ref w, suffix, replacement, 1))
                break;
        return w;
    }

    private static string Step3(string w)
    {
        (string, string)[] map =
        {
            ("icate","ic"), ("ative",""), ("alize","al"), ("iciti","ic"), ("ical","ic"), ("ful",""), ("ness","")
        };
        foreach (var (suffix, replacement) in map)
            if (ReplaceIfMeasureAtLeast(ref w, suffix, replacement, 1))
                break;
        return w;
    }

    private static string Step4(string w)
    {
        string[] suffixes =
        {
            "al","ance","ence","er","ic","able","ible","ant","ement","ment","ent",
            "ion","ou","ism","ate","iti","ous","ive","ize"
        };
        foreach (var suffix in suffixes)
        {
            if (EndsWith(w, suffix))
            {
                string stem = w[..^suffix.Length];
                if (suffix == "ion")
                {
                    if (stem.Length > 0 && (stem[^1] == 's' || stem[^1] == 't') && Measure(stem) > 1)
                        return stem;
                }
                else if (Measure(stem) > 1)
                {
                    return stem;
                }
                break;
            }
        }
        return w;
    }

    private static string Step5A(string w)
    {
        if (EndsWith(w, "e"))
        {
            string stem = w[..^1];
            int m = Measure(stem);
            if (m > 1 || (m == 1 && !EndsCvc(stem)))
                return stem;
        }
        return w;
    }

    private static string Step5B(string w)
    {
        if (w.Length > 1 && w.EndsWith("ll", StringComparison.Ordinal) && Measure(w[..^1]) > 1)
            return w[..^1];
        return w;
    }
}
