using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Benner.CognitiveServices.Classification;
using Benner.CognitiveServices.Contracts;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Benner.CognitiveServices.Tests.Classification;

public class ClassificationFileServiceTests
{
    private readonly ITestOutputHelper _output;

    public ClassificationFileServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void Ocr_OnFixturePdfs_Returns_Text_For_Both()
    {
        if (!TryGetTessDataPath(out var tessDataPath))
        {
            _output.WriteLine("Tessdata not found. Skipping test.");
            Assert.True(true);
            return;
        }

        var nfImg = GetFixturePath("ClassificationFileServiceFiles/BoletoAndPdfs/NFImg.pdf");
        var scanned = GetFixturePath("ClassificationFileServiceFiles/BoletoAndPdfs/ScanedInvoice.pdf");
        if (nfImg is null || scanned is null)
        {
            _output.WriteLine("Fixture PDFs not found. Skipping test.");
            Assert.True(true);
            return;
        }

        var svc = CreateService(tessDataPath, "por+eng");
        var sanitized = BuildSanitized((nfImg, "application/pdf"), (scanned, "application/pdf"));
        var result = svc.Process(sanitized);

        result.Files.Should().HaveCount(2);
        foreach (var f in result.Files)
        {
            var text = f.TextContent ?? string.Empty;
            DumpText(f.FileName, text);
            text.Should().NotBeNullOrWhiteSpace();
        }
    }
    [Fact]
    public void Ocr_OnImage_Returns_Text_When_Tessdata_Available()
    {
        if (!TryGetTessDataPath(out var tessDataPath))
        {
            _output.WriteLine("Tessdata not found. Skipping test.");
            Assert.True(true);
            return;
        }

        var imgPath = CreateTestImageWithText("HELLO OCR", 460, 140);
        try
        {
            var sanitized = new SanitizedFiles
            {
                AcceptedFiles =
                {
                    new SanitizedFile
                    {
                        FileName = Path.GetFileName(imgPath),
                        FullPath = imgPath,
                        MimeType = "image/png"
                    }
                }
            };

            var svc = CreateService(tessDataPath, "eng", usePdfExtractor: false);

            var result = svc.Process(sanitized);

            result.Should().NotBeNull();
            result.Files.Should().HaveCount(1);
            var text = result.Files[0].TextContent ?? string.Empty;
            DumpText("Image_OCR", text);
            text.ToUpperInvariant().Should().Contain("HELLO");
        }
        finally
        {
            TryDelete(imgPath);
        }
    }

    private static bool TryGetTessDataPath(out string path)
    {
        var env = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env!, "eng.traineddata")))
        {
            path = env!;
            return true;
        }
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "tessdata");
            if (File.Exists(Path.Combine(candidate, "eng.traineddata")))
            {
                path = candidate;
                return true;
            }
        }
        path = string.Empty;
        return false;
    }

    private static string? GetFixturePath(string relative)
    {
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var repoPath = Path.Combine(repoRoot, "tests", "Benner.CognitiveServices.Tests", "Fixtures", relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(repoPath)) return repoPath;

        var outPath = Path.Combine(baseDir, "Fixtures", relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(outPath)) return outPath;

        return null;
    }

    private static ClassificationFileService CreateService(string tessDataPath, string languages, bool usePdfExtractor = true)
    {
        IPdfTextExtractor pdfExtractor = usePdfExtractor ? new IText7PdfTextExtractor() : new DummyPdfExtractor();
        IOcrService ocr = new TesseractOcrService(tessDataPath, languages);
        return new ClassificationFileService(pdfExtractor, ocr);
    }

    private static SanitizedFiles BuildSanitized(params (string fullPath, string mime)[] files)
    {
        var s = new SanitizedFiles();
        foreach (var (fullPath, mime) in files)
        {
            s.AcceptedFiles.Add(new SanitizedFile
            {
                FileName = Path.GetFileName(fullPath),
                FullPath = fullPath,
                MimeType = mime
            });
        }
        return s;
    }

    private static string CreateTestImageWithText(string text, int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        using var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        using var font = new Font(FontFamily.GenericSansSerif, 36, FontStyle.Bold);
        using var brush = new SolidBrush(Color.Black);
        g.DrawString(text, font, brush, new PointF(10, height / 3f));
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private static void TryDelete(string path)
    {
        try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); } catch { }
    }

    private sealed class DummyPdfExtractor : IPdfTextExtractor
    {
        public string ExtractText(string filePath) => string.Empty;
    }

    private void DumpText(string label, string text)
    {
        var preview = text.Length > 2000 ? text.Substring(0, 2000) + "..." : text;
        _output.WriteLine($"[{label}] length={text.Length}");
        _output.WriteLine(preview);
        if (!string.IsNullOrEmpty(text))
        {
            var path = SaveTextToCaseFolder(label, text);
            _output.WriteLine($"[{label}] saved to: {path}");
        }
    }

    private static string SaveTextToCaseFolder(string label, string text)
    {
        var dir = ResolveCaseOutputFolder();
        Directory.CreateDirectory(dir);
        var safe = Sanitize(label);
        var file = Path.Combine(dir, $"bcs_ocr_{safe}.txt");
        File.WriteAllText(file, text);
        return file;
    }

    private static string Sanitize(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                chars[i] = '_';
        }
        return new string(chars);
    }

    private static string ResolveCaseOutputFolder()
    {
        // Locate repo root and fixtures folder: tests/Benner.CognitiveServices.Tests/Fixtures/ClassificationFileServiceFiles/Results
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures", "ClassificationFileServiceFiles", "Results");
            var parent = Path.GetDirectoryName(candidate);
            if (!string.IsNullOrEmpty(parent))
            {
                // If Fixtures folder exists at this level, we consider it the repo root
                var fixtures = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures");
                if (Directory.Exists(fixtures))
                    return candidate;
            }
        }
        // Fallback to temp if not found
        return Path.Combine(Path.GetTempPath(), "bcs_test_outputs");
    }
}

