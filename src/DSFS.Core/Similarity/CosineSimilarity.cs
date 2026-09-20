namespace DSFS.Core.Similarity;

/// <summary>
/// Similarity Measure module (section 3.3.4.1): cos(a,b) = (a . b) / (|a| * |b|).
/// Vectors are the sparse TF-IDF weight dictionaries produced by <see cref="DSFS.Core.Preprocessing.TfIdfVectorizer"/>.
/// The result lies in [0, 1] since TF-IDF weights are non-negative, matching the thesis's stated range.
/// </summary>
public static class CosineSimilarity
{
    public static double Compute(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0.0;

        // Iterate the smaller dictionary for the dot product.
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);

        double dot = 0.0;
        foreach (var (term, weight) in small)
        {
            if (large.TryGetValue(term, out var otherWeight))
                dot += weight * otherWeight;
        }

        double normA = Math.Sqrt(a.Values.Sum(v => v * v));
        double normB = Math.Sqrt(b.Values.Sum(v => v * v));

        if (normA == 0.0 || normB == 0.0) return 0.0;

        double similarity = dot / (normA * normB);
        // Guard against tiny floating point overshoot beyond the theoretical [0,1] bound.
        return Math.Clamp(similarity, 0.0, 1.0);
    }

    /// <summary>Cosine distance = 1 - cosine similarity, used by the clustering module.</summary>
    public static double Distance(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b)
        => 1.0 - Compute(a, b);
}
