using System;
using System.IO;
using System.Linq;
using Benner.CognitiveServices.ClassificationType;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Benner.CognitiveServices.Tests.ClassificationType;

public class ClassificationFileTypeDetecBoletoTests
{
    private readonly ITestOutputHelper _output;

    public ClassificationFileTypeDetecBoletoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Use_Text_Files_To_Test_DetectBoleto()
    {
        // Arrange: resolve fixture TXT files saved under DetecBoleto
        var fixtureDir = GetFixtureDetecBoletoFolder();
        var txtFiles = Directory.GetFiles(fixtureDir, "*.txt", SearchOption.TopDirectoryOnly);
        txtFiles.Should().NotBeEmpty("expected extracted .txt contents in DetecBoleto folder");

        // Instantiate classifier and call public DetectBoleto directly
        var classifier = new ClassificationFileType();

        // Act + Assert per file
        foreach (var txt in txtFiles.OrderBy(f => f))
        {
            var content = File.ReadAllText(txt);
            var isBoleto = classifier.DetectBoleto(content);

            var fileName = Path.GetFileName(txt);
            _output.WriteLine($"Testing {fileName}: len={content.Length}, boleto={isBoleto}");

            if (fileName.Contains("Boleto", StringComparison.OrdinalIgnoreCase))
            {
                isBoleto.Should().BeTrue($"{fileName} should be detected as boleto");
            }
            else
            {
                isBoleto.Should().BeFalse($"{fileName} should not be detected as boleto");
            }
        }
    }

    private static string GetFixtureDetecBoletoFolder()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures", "ClassificationFileTypeFiles", "DetecBoleto");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("DetecBoleto fixtures folder not found.");
    }

}
