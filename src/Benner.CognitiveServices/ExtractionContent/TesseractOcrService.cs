using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using Tesseract;
using Docnet.Core;
using Docnet.Core.Models;

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
            // continue to fallback
        }

        var accumulated = sb.ToString();
        if (!string.IsNullOrWhiteSpace(accumulated)) return accumulated;

        // Fallback: render full pages via PDFium (Docnet)
        try
        {
            var rendered = RenderAndOcrWithPdfium(pdfPath);
            if (!string.IsNullOrWhiteSpace(rendered))
                return rendered;
        }
        catch { /* ignore */ }

        return string.Empty;
    }

    private static IEnumerable<byte[]> ExtractImages(PdfPage page)
    {
        var results = new List<byte[]>();
        try
        {
            ExtractFromResources(page.GetResources(), results);
            ExtractFromCanvas(page, results);
        }
        catch { /* ignore */ }

        return results;
    }

    private static void ExtractFromResources(iText.Kernel.Pdf.PdfResources? resources, List<byte[]> results)
    {
        if (resources is null) return;

        try
        {
            var xObjects = resources.GetResource(PdfName.XObject) as PdfDictionary;
            if (xObjects == null) return;

            foreach (var name in xObjects.KeySet())
            {
                var stream = xObjects.GetAsStream(name);
                if (stream == null) continue;
                var subtype = stream.GetAsName(PdfName.Subtype);

                if (PdfName.Image.Equals(subtype))
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
                else if (PdfName.Form.Equals(subtype))
                {
                    try
                    {
                        var form = new PdfFormXObject(stream);
                        var inner = form.GetResources();
                        ExtractFromResources(inner, results);
                        try
                        {
                            var listener = new ImageEventListener(results);
                            var processor = new PdfCanvasProcessor(listener);
                            var contentBytes = form.GetPdfObject().GetBytes();
                            processor.ProcessContent(contentBytes, form.GetResources());
                        }
                        catch { /* ignore */ }
                    }
                    catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }
    }

    private static void ExtractFromCanvas(PdfPage page, List<byte[]> results)
    {
        try
        {
            var listener = new ImageEventListener(results);
            var processor = new PdfCanvasProcessor(listener);
            processor.ProcessPageContent(page);
        }
        catch { /* ignore */ }
    }

    private sealed class ImageEventListener : IEventListener
    {
        private readonly List<byte[]> _results;
        public ImageEventListener(List<byte[]> results) => _results = results;

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_IMAGE) return;
            try
            {
                var renderInfo = (ImageRenderInfo)data;
                var img = renderInfo.GetImage();
                if (img == null) return;
                var bytes = img.GetImageBytes(true);
                if (bytes is { Length: > 0 }) _results.Add(bytes);
            }
            catch { /* ignore */ }
        }

        public System.Collections.Generic.ICollection<EventType> GetSupportedEvents()
            => new[] { EventType.RENDER_IMAGE };
    }

    private string RenderAndOcrWithPdfium(string pdfPath)
    {
        var sb = new StringBuilder();
        try
        {
            using var lib = DocLib.Instance;
            // target dimensions approximating ~300 DPI for common page sizes
            var dims = new PageDimensions(1080, 1440);
            using var doc = lib.GetDocReader(pdfPath, dims);
            var pages = doc.GetPageCount();
            for (int i = 0; i < pages; i++)
            {
                using var pageReader = doc.GetPageReader(i);
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var raw = pageReader.GetImage(); // BGRA32

                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
                try
                {
                    using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(raw, 0, bmpData.Scan0, raw.Length);
                    }
                    finally
                    {
                        bmp.UnlockBits(bmpData);
                    }

                    bmp.Save(temp, System.Drawing.Imaging.ImageFormat.Png);
                    var text = ReadTextFromImage(temp);
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                finally
                {
                    try { File.Delete(temp); } catch { }
                }
            }
        }
        catch { /* ignore */ }

        return sb.ToString();
    }
}

