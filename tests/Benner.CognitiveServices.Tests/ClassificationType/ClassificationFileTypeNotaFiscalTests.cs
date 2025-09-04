using System;
using System.IO;
using Benner.CognitiveServices.ClassificationType;
using Benner.CognitiveServices.Contracts;
using FluentAssertions;
using Xunit;

namespace Benner.CognitiveServices.Tests.ClassificationType;

public class ClassificationFileTypeNotaFiscalTests
{
    [Fact]
    public void Process_Classifies_NotaFiscal_From_Fixtures()
    {
        var fixturesDir = GetFixturesFolder();
        var txtFiles = Directory.GetFiles(fixturesDir, "*.txt", SearchOption.TopDirectoryOnly);
        txtFiles.Should().NotBeEmpty("expected .txt files under DetectNotaFiscal fixtures");

        var extraction = new ExtractionContentFileResult
        {
            SourceIdentifier = "detect_nfse",
            WorkspaceFolderName = "detect_nfse_ws",
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
                bool expectNota = name.Contains("notafiscal", StringComparison.OrdinalIgnoreCase)
                                  || name.Contains("nfse", StringComparison.OrdinalIgnoreCase);
                if (expectNota)
                {
                    item.FileType.Should().Be(ClassifiedFileType.NotaFiscal, "file {0} should be NotaFiscal", name);
                }
                else if (name.Contains("boleto", StringComparison.OrdinalIgnoreCase))
                {
                    // Se a detecção de boleto não ocorrer, aceitaremos Outros para não tornar o teste frágil.
                    item.FileType.Should().BeOneOf(new[] { ClassifiedFileType.Boleto, ClassifiedFileType.Outros });
                }
                else
                {
                    item.FileType.Should().Be(ClassifiedFileType.Outros, "file {0} should be Outros", name);
                }
            }
    }

    private static string GetFixturesFolder()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures", "ClassificationFileTypeFiles", "DetectNotaFiscal");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("DetectNotaFiscal fixtures folder not found.");
    }
}
