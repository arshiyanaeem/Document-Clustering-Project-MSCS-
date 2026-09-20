using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace DSFS.Core.TextExtraction;

/// <summary>
/// Handles .docx research publication files. A .docx is a zip archive containing
/// word/document.xml with the body text; this reads it using only
/// System.IO.Compression and System.Xml.Linq from the base class library, so no
/// extra NuGet package is required to open a Word research paper.
/// </summary>
public class DocxTextExtractor : IDocumentTextExtractor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public IEnumerable<string> SupportedExtensions => new[] { ".docx" };

    public string ExtractText(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("Not a valid .docx file (word/document.xml missing).");

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);

        var sb = new StringBuilder();
        // Each <w:p> is a paragraph; each <w:t> inside it is a run of text.
        foreach (var paragraph in doc.Descendants(W + "p"))
        {
            foreach (var textNode in paragraph.Descendants(W + "t"))
                sb.Append(textNode.Value);
            sb.Append(Environment.NewLine);
        }
        return sb.ToString();
    }
}
