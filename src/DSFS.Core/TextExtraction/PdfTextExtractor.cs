using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DSFS.Core.TextExtraction;

/// <summary>
/// Handles .pdf research publication files (the primary input format described in
/// section 5.4.1.1 "INPUT COMPONENTS": "The pdf format research documents are the
/// input in the phase of data collection module.").
///
/// PDF text layout is a genuinely complex binary/stream format. This extractor uses a
/// two-tier strategy that needs zero extra NuGet packages:
///
///   1. If the poppler "pdftotext" command-line tool is available on PATH (it ships with
///      most Linux distros and is easy to install on Windows/macOS), it is used - this
///      gives production-quality extraction, including for compressed/complex PDFs.
///   2. Otherwise, a lightweight built-in fallback scans the raw PDF bytes for
///      uncompressed text-showing operators (Tj / TJ inside BT...ET blocks). This
///      correctly handles simple, uncompressed PDFs (e.g. many "Word > Save as PDF"
///      exports) but will not decode PDFs whose content streams are Flate-compressed.
///
/// For full robustness in production, replace/augment this class with the PdfPig NuGet
/// package (`dotnet add package UglyToad.PdfPig`) and call
/// <c>string.Join(" ", PdfDocument.Open(filePath).GetPages().Select(p => p.Text))</c>.
/// </summary>
public class PdfTextExtractor : IDocumentTextExtractor
{
    public IEnumerable<string> SupportedExtensions => new[] { ".pdf" };

    public string ExtractText(string filePath)
    {
        var viaTool = TryExtractWithPdfToText(filePath);
        if (!string.IsNullOrWhiteSpace(viaTool))
            return viaTool!;

        return ExtractWithBuiltInFallback(filePath);
    }

    private static string? TryExtractWithPdfToText(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pdftotext",
                Arguments = $"-layout \"{filePath}\" -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null) return null;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            // pdftotext not installed, or blocked - fall back silently.
            return null;
        }
    }

    private static readonly Regex TextShowOperator = new(@"\((?:[^()\\]|\\.)*\)\s*T[jJ]", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ParenLiteral = new(@"\((?:[^()\\]|\\.)*\)", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Naive fallback: pulls literal-string operands of Tj/TJ text-showing operators out of
    /// the raw file bytes. Works for simple, uncompressed content streams only.
    /// </summary>
    private static string ExtractWithBuiltInFallback(string filePath)
    {
        string raw = File.ReadAllText(filePath, Encoding.Latin1);
        var sb = new StringBuilder();

        foreach (Match m in TextShowOperator.Matches(raw))
        {
            foreach (Match lit in ParenLiteral.Matches(m.Value))
            {
                string content = lit.Value[1..^1];
                content = content
                    .Replace("\\(", "(").Replace("\\)", ")").Replace("\\\\", "\\")
                    .Replace("\\n", "\n").Replace("\\r", "\r");
                sb.Append(content);
                sb.Append(' ');
            }
            sb.Append('\n');
        }

        string result = sb.ToString().Trim();
        if (result.Length == 0)
        {
            throw new NotSupportedException(
                "Could not extract text from this PDF with the built-in fallback extractor " +
                "(its content stream is likely compressed). Install poppler's 'pdftotext' " +
                "utility, or add the PdfPig NuGet package, for full PDF support.");
        }
        return result;
    }
}
