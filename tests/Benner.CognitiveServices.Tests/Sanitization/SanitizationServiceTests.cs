using System;
using System.IO;
using System.Linq;
using Benner.CognitiveServices.Contracts;
using Benner.CognitiveServices.Sanitization;
using FluentAssertions;
using Xunit;

namespace Benner.CognitiveServices.Tests.Sanitization;

public class SanitizationServiceTests
{
    [Fact]
    public void Process_Should_Accept_Pdf_And_NonSignatureImages_And_Discard_ThumbsAndSignatures()
    {
        // Arrange
        var scenarioPath = GetScenarioPath("SanitizationServiceFiles", "ThumbsSignature_BoletoAndPdfs");

        var prepared = new PreparedWorkspace
        {
            WorkspaceFolderName = "test_scenario",
            WorkspaceFullPath = scenarioPath,
            Files = Directory.GetFiles(scenarioPath)
                .Select(fp => new WorkspaceFile { FileName = Path.GetFileName(fp), FullPath = fp })
                .ToList()
        };

        // No dependency on preparation service; sanitization uses only PreparedWorkspace
        var service = new SanitizationService();

        // Act
        var result = service.Process(prepared);

        // Assert
        result.AcceptedFiles.Select(f => f.FileName).Should().BeEquivalentTo(new[]
        {
            "boleto.png",
            "fatura.pdf",
            "nota_fiscal.pdf"
        });

        result.DiscardedFiles.Should().HaveCount(2);
        result.DiscardedFiles.Select(d => d.FileName).Should().BeEquivalentTo(new[]
        {
            "signature_logo.png",
            "thumb_avatar.jpg"
        });

        result.DiscardedFiles.All(d => d.Reason is "Likely email signature/thumbnail" or "Image too small (likely non-text)" or "Unsupported file type").Should().BeTrue();
    }

    private static string GetScenarioPath(string classFilesFolder, string scenarioFolder)
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "Fixtures", classFilesFolder, scenarioFolder);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Scenario folder not found: {path}");
        return path;
    }
}
