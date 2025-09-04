using System;
using System.IO;
using System.Linq;
using Benner.CognitiveServices.ClassificationType;
using Benner.CognitiveServices.Contracts;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Benner.CognitiveServices.Tests.ClassificationType;

public class ClassificationFileTypeProcessTests
{
    private readonly ITestOutputHelper _output;

    public ClassificationFileTypeProcessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Process_Classifies_Boleto_And_Others_From_Text_Fixtures()
    {
        // Arrange: read text fixtures
        var dir = GetProcessFixturesFolder();
        var txtFiles = Directory.GetFiles(dir, "*.txt", SearchOption.TopDirectoryOnly);
        txtFiles.Should().NotBeEmpty("expected .txt files under Process fixtures");

        var extracted = new ExtractionContentFileResult
        {
            SourceIdentifier = "process_test",
            WorkspaceFolderName = "process_test_ws",
            WorkspaceFullPath = dir
        };

        foreach (var file in txtFiles)
        {
            extracted.Files.Add(new FileContentExtraction
            {
                FileName = Path.GetFileName(file),
                FullPath = file,
                FileType = "text/plain",
                TextContent = File.ReadAllText(file)
            });
        }

        var classifier = new ClassificationFileType();

        // Act
        var result = classifier.Process(extracted);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(txtFiles.Length);

        foreach (var item in result.Files.OrderBy(f => f.FileName))
        {
            _output.WriteLine($"{item.FileName} => {item.FileType}");
            if (item.FileName.Contains("Boleto", StringComparison.OrdinalIgnoreCase))
            {
                item.FileType.Should().Be(ClassifiedFileType.Boleto);
            }
            else
            {
                item.FileType.Should().Be(ClassifiedFileType.Outros);
            }
        }
    }

    private static string GetProcessFixturesFolder()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures", "ClassificationFileTypeFiles", "Process");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("Process fixtures folder not found.");
    }
}

