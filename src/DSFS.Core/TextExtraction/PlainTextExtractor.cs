namespace DSFS.Core.TextExtraction;

/// <summary>Handles .txt research publication files.</summary>
public class PlainTextExtractor : IDocumentTextExtractor
{
    public IEnumerable<string> SupportedExtensions => new[] { ".txt" };

    public string ExtractText(string filePath) => File.ReadAllText(filePath);
}
