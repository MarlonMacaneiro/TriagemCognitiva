using System;
using System.IO;
using System.Linq;
using Benner.CognitiveServices.Contracts;
using Benner.CognitiveServices.ExtractionContent;
using Xunit;
using Xunit.Abstractions;

namespace Benner.CognitiveServices.Tests.ExtractionContent;

public class ExtractionContentFileServiceExtractCasesTests
{
    private readonly ITestOutputHelper _output;

    public ExtractionContentFileServiceExtractCasesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Extract_Request_Files_If_Response_Missing()
    {
        var (requestDir, responseDir) = GetExtractCasesFolders();
        Directory.CreateDirectory(responseDir);

        var files = Directory.GetFiles(requestDir);
        if (files.Length == 0)
        {
            _output.WriteLine("No request files found. Nothing to do.");
            return;
        }

        // Initialize services
        IPdfTextExtractor pdfExtractor = new IText7PdfTextExtractor();

        IOcrService ocr;
        if (TryGetTessDataPath(out var tessDataPath))
        {
            ocr = new TesseractOcrService(tessDataPath, "por+eng");
        }
        else
        {
            _output.WriteLine("Tessdata not found. OCR may return empty for images/scanned PDFs.");
            ocr = new TesseractOcrService(Path.GetTempPath(), "por+eng");
        }

        var svc = new ExtractionContentFileService(pdfExtractor, ocr);

        foreach (var path in files.OrderBy(p => p))
        {
            var baseName = Path.GetFileNameWithoutExtension(path);
            var outPath = Path.Combine(responseDir, baseName + ".txt");
            if (File.Exists(outPath))
            {
                _output.WriteLine($"Skip existing: {outPath}");
                continue;
            }

            var sanitized = new SanitizedFiles
            {
                AcceptedFiles =
                {
                    new SanitizedFile
                    {
                        FileName = Path.GetFileName(path),
                        FullPath = path,
                        MimeType = GuessMimeType(path)
                    }
                }
            };

            var result = svc.Process(sanitized);
            var text = result.Files.FirstOrDefault()?.TextContent ?? string.Empty;
            File.WriteAllText(outPath, text);
            _output.WriteLine($"Saved: {outPath} (len={text.Length})");
        }
    }

    private static string GuessMimeType(string fullPath)
    {
        var ext = (Path.GetExtension(fullPath) ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tif" or ".tiff" => "image/tiff",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static (string requestDir, string responseDir) GetExtractCasesFolders()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var rootFixtures = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures");
            var req = Path.Combine(rootFixtures, "ExtractionContentFileServiceFiles", "ExtractFileContetCases", "Request");
            var res = Path.Combine(rootFixtures, "ExtractionContentFileServiceFiles", "ExtractFileContetCases", "Response");
            if (Directory.Exists(req)) return (req, res);
        }
        throw new DirectoryNotFoundException("ExtractFileContetCases/Request folder not found.");
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
}

