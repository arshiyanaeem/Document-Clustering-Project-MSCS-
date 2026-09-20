namespace DSFS.Core.TextExtraction;

/// <summary>Resolves the correct <see cref="IDocumentTextExtractor"/> for a given file path.</summary>
public static class DocumentTextExtractorFactory
{
    private static readonly List<IDocumentTextExtractor> Extractors = new()
    {
        new PlainTextExtractor(),
        new DocxTextExtractor(),
        new PdfTextExtractor()
    };

    public static string ExtractText(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        var extractor = Extractors.FirstOrDefault(e =>
            e.SupportedExtensions.Any(s => s.Equals(ext, StringComparison.OrdinalIgnoreCase)));

        if (extractor is null)
            throw new NotSupportedException($"Unsupported research publication file type: '{ext}'. Supported: .txt, .docx, .pdf");

        return extractor.ExtractText(filePath);
    }

    public static bool IsSupported(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        return Extractors.Any(e => e.SupportedExtensions.Any(s => s.Equals(ext, StringComparison.OrdinalIgnoreCase)));
    }
}
