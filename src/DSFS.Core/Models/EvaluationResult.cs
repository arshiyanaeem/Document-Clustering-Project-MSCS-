namespace DSFS.Core.Models;

/// <summary>
/// Holds the evaluation parameters described in Chapter 4 of the thesis
/// (True Positive / False Positive / False Negative, Precision, Recall, F-Measure).
/// </summary>
public class EvaluationResult
{
    public int TruePositive { get; set; }
    public int FalsePositive { get; set; }
    public int FalseNegative { get; set; }

    /// <summary>Precision = TP / (TP + FP).</summary>
    public double Precision { get; set; }

    /// <summary>Recall = TP / (TP + FN).</summary>
    public double Recall { get; set; }

    /// <summary>F-Measure = 2 * (Precision * Recall) / (Precision + Recall).</summary>
    public double FMeasure { get; set; }

    public override string ToString() =>
        $"TP={TruePositive}, FP={FalsePositive}, FN={FalseNegative}, " +
        $"Precision={Precision:P2}, Recall={Recall:P2}, F-Measure={FMeasure:P2}";
}
