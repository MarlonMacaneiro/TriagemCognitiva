using System;
using System.IO;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace Benner.CognitiveServices.ExtractionContent;

public class IText7PdfTextExtractor : IPdfTextExtractor
{
    public string ExtractText(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return string.Empty;

        var sb = new StringBuilder();

        using var pdf = new PdfDocument(new PdfReader(filePath));
        var pages = pdf.GetNumberOfPages();
        for (int i = 1; i <= pages; i++)
        {
            var page = pdf.GetPage(i);
            var strategy = new LocationTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }

        return sb.ToString();
    }
}
