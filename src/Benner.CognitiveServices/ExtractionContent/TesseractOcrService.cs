using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf;
using iText.Kernel;
using Tesseract;

namespace Benner.CognitiveServices.ExtractionContent;

public class TesseractOcrService : IOcrService
{
    private readonly string _tessDataPath;
    private readonly string _languages;

    public TesseractOcrService(string tessDataPath, string languages = "por+eng")
    {
        _tessDataPath = tessDataPath ?? throw new ArgumentNullException(nameof(tessDataPath));
        _languages = string.IsNullOrWhiteSpace(languages) ? "por+eng" : languages;
    }

    public string ReadText(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return string.Empty;

        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            return ReadTextFromPdfImages(filePath);

        return ReadTextFromImage(filePath);
    }

    private string ReadTextFromImage(string imagePath)
    {
        try
        {
            using var engine = new TesseractEngine(_tessDataPath, _languages, EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            return page.GetText() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ReadTextFromPdfImages(string pdfPath)
    {
        var sb = new StringBuilder();

        try
        {
            using var pdf = new PdfDocument(new PdfReader(pdfPath));
            var total = pdf.GetNumberOfPages();
            for (int i = 1; i <= total; i++)
            {
                var page = pdf.GetPage(i);
                foreach (var bytes in ExtractImages(page))
                {
                    var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
                    try
                    {
                        File.WriteAllBytes(temp, bytes);
                        var text = ReadTextFromImage(temp);
                        if (!string.IsNullOrWhiteSpace(text))
                            sb.AppendLine(text);
                    }
                    finally
                    {
                        try { File.Delete(temp); } catch { /* ignore */ }
                    }
                }
            }
        }
        catch
        {
            // Falha ao abrir/ler PDF ou processar imagens -> retornar o que tiver
        }

        return sb.ToString();
    }

    private static IEnumerable<byte[]> ExtractImages(PdfPage page)
    {
        var results = new List<byte[]>();
        try
        {
            ExtractFromResources(page.GetResources(), results);
        }
        catch { /* ignore */ }

        return results;
    }

    private static void ExtractFromResources(iText.Kernel.Pdf.PdfResources? resources, List<byte[]> results)
    {
        if (resources is null) return;

        try
        {
            var xObjects = resources.GetResource(iText.Kernel.Pdf.PdfName.XObject) as iText.Kernel.Pdf.PdfDictionary;
            if (xObjects == null) return;

            foreach (var name in xObjects.KeySet())
            {
                var stream = xObjects.GetAsStream(name);
                if (stream == null) continue;
                var subtype = stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype);

                if (iText.Kernel.Pdf.PdfName.Image.Equals(subtype))
                {
                    try
                    {
                        var img = new PdfImageXObject(stream);
                        var bytes = img.GetImageBytes(true);
                        if (bytes is { Length: > 0 })
                            results.Add(bytes);
                    }
                    catch { /* ignore */ }
                }
                else if (iText.Kernel.Pdf.PdfName.Form.Equals(subtype))
                {
                    try
                    {
                        var form = new PdfFormXObject(stream);
                        var inner = form.GetResources();
                        ExtractFromResources(inner, results);
                    }
                    catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }
    }
}
