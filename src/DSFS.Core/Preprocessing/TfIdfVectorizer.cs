using DSFS.Core.Models;

namespace DSFS.Core.Preprocessing;

/// <summary>
/// Preprocessing step 4 (section 3.3.2.4): converts the bag of stemmed words for every
/// document into a numeric, high-dimensional TF-IDF vector so that the corpus can be
/// handled by the clustering and similarity-measure modules.
///
///   tf(t, d)  = (number of times term t occurs in document d) / (total terms in d)
///   idf(t)    = ln( N / (1 + df(t)) ) + 1          // smoothed inverse document frequency
///   tfidf(t,d)= tf(t, d) * idf(t)
/// </summary>
public class TfIdfVectorizer
{
    /// <summary>The full vocabulary discovered across the corpus, in a stable order.</summary>
    public List<string> Vocabulary { get; private set; } = new();

    private Dictionary<string, double> _idfByTerm = new();

    /// <summary>
    /// Fits the IDF weights from the corpus and writes the resulting TF-IDF vector into
    /// each document's <see cref="DocumentRecord.TfIdfVector"/>.
    /// </summary>
    public void FitTransform(IReadOnlyList<DocumentRecord> documents)
    {
        int n = documents.Count;
        if (n == 0) return;

        // Document frequency: number of documents containing each term at least once.
        var documentFrequency = new Dictionary<string, int>();
        foreach (var doc in documents)
        {
            foreach (var term in doc.ProcessedTokens.Distinct())
            {
                documentFrequency.TryGetValue(term, out var count);
                documentFrequency[term] = count + 1;
            }
        }

        Vocabulary = documentFrequency.Keys.OrderBy(t => t, StringComparer.Ordinal).ToList();

        _idfByTerm = Vocabulary.ToDictionary(
            term => term,
            term => Math.Log((double)n / (1.0 + documentFrequency[term])) + 1.0);

        foreach (var doc in documents)
            doc.TfIdfVector = ComputeVector(doc.ProcessedTokens);
    }

    /// <summary>
    /// Projects an arbitrary token list onto the already-fitted vocabulary/IDF weights.
    /// Useful for scoring a new document against an existing model without refitting.
    /// </summary>
    public Dictionary<string, double> ComputeVector(IReadOnlyList<string> tokens)
    {
        var vector = new Dictionary<string, double>();
        if (tokens.Count == 0) return vector;

        var termFrequency = new Dictionary<string, int>();
        foreach (var term in tokens)
        {
            termFrequency.TryGetValue(term, out var count);
            termFrequency[term] = count + 1;
        }

        foreach (var (term, rawCount) in termFrequency)
        {
            if (!_idfByTerm.TryGetValue(term, out var idf))
                continue; // term outside the fitted vocabulary is ignored

            double tf = (double)rawCount / tokens.Count;
            vector[term] = tf * idf;
        }
        return vector;
    }
}
