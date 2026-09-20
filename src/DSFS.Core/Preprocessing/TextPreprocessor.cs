using DSFS.Core.Models;

namespace DSFS.Core.Preprocessing;

/// <summary>
/// Orchestrates the full Preprocessing of Data Module (section 5.4.1.2 / Figure 9):
/// Tokenization -&gt; Stop Word Removal -&gt; Stemming (Porter) -&gt; TF-IDF.
/// </summary>
public class TextPreprocessor
{
    private readonly PorterStemmer _stemmer = new();
    private readonly TfIdfVectorizer _vectorizer = new();

    public TfIdfVectorizer Vectorizer => _vectorizer;

    /// <summary>Runs tokenization, stop word removal and stemming for a single document (steps 1-3).</summary>
    public List<string> ProcessTokensOnly(string rawText)
    {
        var tokens = Tokenizer.Tokenize(rawText);
        var filtered = StopWordRemover.RemoveStopWords(tokens);
        return _stemmer.StemAll(filtered);
    }

    /// <summary>
    /// Runs the complete four-step pipeline over the whole corpus, populating
    /// <see cref="DocumentRecord.ProcessedTokens"/> and <see cref="DocumentRecord.TfIdfVector"/>.
    /// </summary>
    public void ProcessCorpus(IReadOnlyList<DocumentRecord> documents)
    {
        foreach (var doc in documents)
            doc.ProcessedTokens = ProcessTokensOnly(doc.RawText);

        _vectorizer.FitTransform(documents);
    }
}
