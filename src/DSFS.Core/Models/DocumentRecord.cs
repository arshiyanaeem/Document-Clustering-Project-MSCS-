namespace DSFS.Core.Models;

/// <summary>
/// Represents a single research publication as it flows through the DSFS pipeline:
/// Data Collection -&gt; Preprocessing -&gt; Clustering -&gt; Similarity Measure
/// (see Figure 7, "A Desktop Scholar Facilitator System Architecture", in the thesis).
/// </summary>
public class DocumentRecord
{
    /// <summary>Unique identifier for the document within the current session.</summary>
    public int Id { get; set; }

    /// <summary>Original file name as imported by the Data Collection module.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Research scholar associated with this publication.</summary>
    public string ScholarName { get; set; } = string.Empty;

    /// <summary>Research domain / subject area of the publication (used as the ground-truth class label).</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Raw, unstructured text extracted from the source file (pdf/docx/txt).</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>Tokens remaining after tokenization + stop word removal + Porter stemming.</summary>
    public List<string> ProcessedTokens { get; set; } = new();

    /// <summary>TF-IDF weighted numeric vector produced by the Preprocessing module. Keyed by vocabulary term.</summary>
    public Dictionary<string, double> TfIdfVector { get; set; } = new();

    /// <summary>Id of the cluster this document was assigned to after the Clustering module ran (-1 = not yet clustered).</summary>
    public int ClusterId { get; set; } = -1;

    public override string ToString() => $"{FileName} ({ScholarName} / {Domain})";
}
