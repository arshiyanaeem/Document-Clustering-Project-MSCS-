namespace DSFS.Core.TextExtraction;

/// <summary>
/// Data Collection Module abstraction (section 5.4.1.1): converts a source research-publication
/// file into plain text so it can enter the Preprocessing pipeline.
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>File extensions this extractor can handle, e.g. ".txt".</summary>
    IEnumerable<string> SupportedExtensions { get; }

    string ExtractText(string filePath);
}
