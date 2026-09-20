using DSFS.Core.Models;

namespace DSFS.Core.Evaluation;

/// <summary>
/// Performance Evaluation module (Chapter 4). Implements the pairwise definition of
/// True Positive / False Positive / False Negative that the thesis describes in words
/// (section 4.1.2): two documents are considered a "true match" when they belong to the
/// same scholar AND the same research domain. A pair is then:
///
///   TP - a true match that the algorithm placed in the same cluster,
///   FP - NOT a true match (e.g. same domain, different scholar) that ended up in the same cluster,
///   FN - a true match that the algorithm split across different clusters.
///
/// Precision, Recall and F-Measure are computed exactly as in Table 5 of the thesis.
/// </summary>
public static class ClusterEvaluator
{
    public static EvaluationResult EvaluatePairwise(IReadOnlyList<DocumentRecord> documents)
    {
        int tp = 0, fp = 0, fn = 0;

        for (int i = 0; i < documents.Count; i++)
        {
            for (int j = i + 1; j < documents.Count; j++)
            {
                var a = documents[i];
                var b = documents[j];

                bool isTrueMatch = a.ScholarName.Equals(b.ScholarName, StringComparison.OrdinalIgnoreCase)
                                    && a.Domain.Equals(b.Domain, StringComparison.OrdinalIgnoreCase);
                bool sameCluster = a.ClusterId == b.ClusterId && a.ClusterId >= 0;

                if (isTrueMatch && sameCluster) tp++;
                else if (!isTrueMatch && sameCluster) fp++;
                else if (isTrueMatch && !sameCluster) fn++;
                // true negatives (not a match, different clusters) are correct but not counted,
                // matching the thesis's Table 5 formulas which only use TP, FP and FN.
            }
        }

        double precision = (tp + fp) > 0 ? (double)tp / (tp + fp) : 0.0;
        double recall = (tp + fn) > 0 ? (double)tp / (tp + fn) : 0.0;
        double fMeasure = (precision + recall) > 0 ? 2 * precision * recall / (precision + recall) : 0.0;

        return new EvaluationResult
        {
            TruePositive = tp,
            FalsePositive = fp,
            FalseNegative = fn,
            Precision = precision,
            Recall = recall,
            FMeasure = fMeasure
        };
    }
}
