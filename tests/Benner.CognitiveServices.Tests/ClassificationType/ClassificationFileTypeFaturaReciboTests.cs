using System;
using System.IO;
using Benner.CognitiveServices.ClassificationType;
using Benner.CognitiveServices.Contracts;
using FluentAssertions;
using Xunit;

namespace Benner.CognitiveServices.Tests.ClassificationType;

public class ClassificationFileTypeFaturaReciboTests
{
    [Fact]
    public void Process_Classifies_FaturaRecibo_From_Fixtures()
    {
        var fixturesDir = GetFixturesFolder();
        var txtFiles = Directory.GetFiles(fixturesDir, "*.txt", SearchOption.TopDirectoryOnly);
        txtFiles.Should().NotBeEmpty("expected .txt files under DetectFaturaRecibo fixtures");

        var extraction = new ExtractionContentFileResult
        {
            SourceIdentifier = "detect_fatura_recibo",
            WorkspaceFolderName = "detect_fatura_recibo_ws",
            WorkspaceFullPath = fixturesDir
        };

        foreach (var file in txtFiles)
        {
            extraction.Files.Add(new FileContentExtraction
            {
                FileName = Path.GetFileName(file),
                FullPath = file,
                FileType = "text/plain",
                TextContent = File.ReadAllText(file)
            });
        }

        var classifier = new ClassificationFileType();
        var result = classifier.Process(extraction);

        result.Files.Should().HaveCount(txtFiles.Length);

        foreach (var item in result.Files)
        {
            var name = item.FileName;
            bool expectFaturaRecibo = name.Contains("FaturaRecibo", StringComparison.OrdinalIgnoreCase)
                                      || name.Contains("RPS", StringComparison.OrdinalIgnoreCase)
                                      || name.Contains("Recibo", StringComparison.OrdinalIgnoreCase);
            if (expectFaturaRecibo)
            {
                item.FileType.Should().Be(ClassifiedFileType.FaturaRecibo, $"file {name} should be FaturaRecibo");
            }
        }
    }

    private static string GetFixturesFolder()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures", "ClassificationFileTypeFiles", "DetectFaturaRecibo");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("DetectFaturaRecibo fixtures folder not found.");
    }
}
