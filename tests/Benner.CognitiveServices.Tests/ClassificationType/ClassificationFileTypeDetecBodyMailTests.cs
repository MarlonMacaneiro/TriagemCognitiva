using System;
using System.IO;
using System.Linq;
using Benner.CognitiveServices.ClassificationType;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Benner.CognitiveServices.Tests.ClassificationType;

public class ClassificationFileTypeDetecBodyMailTests
{
    private readonly ITestOutputHelper _output;

    public ClassificationFileTypeDetecBodyMailTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Use_Text_Files_To_Test_DetectBodyMail()
    {
        // Arrange: resolve fixture TXT files saved under DetecBodyMail
        var fixtureDir = GetFixtureDetecBodyMailFolder();
        var txtFiles = Directory.GetFiles(fixtureDir, "*.txt", SearchOption.TopDirectoryOnly);
        txtFiles.Should().NotBeEmpty("expected extracted .txt contents in DetecBodyMail folder");

        var classifier = new ClassificationFileType();

        // Act + Assert per file
        foreach (var txt in txtFiles.OrderBy(f => f))
        {
            var content = File.ReadAllText(txt);
            var isBodyMail = classifier.DetectBodyMail(content);

            var fileName = Path.GetFileName(txt);
            // Debug helpers to understand scoring
            var headerCount = CountHeaderLikeLines(content);
            _output.WriteLine($"Testing {fileName}: len={content.Length}, bodyMail={isBodyMail}, headerCount={headerCount}");

            if (fileName.Contains("BodyMail", StringComparison.OrdinalIgnoreCase))
            {
                isBodyMail.Should().BeTrue($"{fileName} should be detected as body mail");
            }
            else
            {
                isBodyMail.Should().BeFalse($"{fileName} should not be detected as body mail");
            }
        }
    }

    private static int CountHeaderLikeLines(string text)
    {
        var normalized = text.Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var headerStartRegex = new System.Text.RegularExpressions.Regex(
            @"^\s*(de|from|para|to|assunto|subject|cc|cco|bcc|data|sent|enviado|enviada|reply\-to|responder\s+para|encaminhada\s+de|forwarded\s+from)\s*:?.*",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        int count = 0;
        foreach (var rawLine in lines.Take(60))
        {
            var line = rawLine.Trim();
            if (headerStartRegex.IsMatch(line)) count++;
        }
        return count;
    }

    private static string GetFixtureDetecBodyMailFolder()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Benner.CognitiveServices.Tests", "Fixtures", "ClassificationFileTypeFiles", "DetecBodyMail");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("DetecBodyMail fixtures folder not found.");
    }
}

