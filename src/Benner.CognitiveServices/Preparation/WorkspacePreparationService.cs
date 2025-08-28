using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.Preparation;

public class WorkspacePreparationService : IPreparationService
{
    private readonly string _rootDirectory;

    public WorkspacePreparationService(string? rootDirectory = null)
    {
        _rootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(Path.GetTempPath(), "BCS")
            : rootDirectory!;
    }

    public PreparedWorkspace Prepare(SanitizationRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        Directory.CreateDirectory(_rootDirectory);

        var folderName = BuildWorkspaceFolderName(request.SourceIdentifier);
        var workspacePath = Path.Combine(_rootDirectory, folderName);
        Directory.CreateDirectory(workspacePath);

        var result = new PreparedWorkspace
        {
            WorkspaceFolderName = folderName,
            WorkspaceFullPath = workspacePath,
            Files = new List<WorkspaceFile>()
        };

        if (request.Files is null || request.Files.Count == 0)
            return result;

        foreach (var file in request.Files)
        {
            if (file is null) continue;

            var safeName = EnsureUniqueName(workspacePath, SanitizeFileName(file.FileName));

            try
            {
                var bytes = Convert.FromBase64String(file.FileContentBase64 ?? string.Empty);
                var fullPath = Path.Combine(workspacePath, safeName);
                File.WriteAllBytes(fullPath, bytes);

                result.Files.Add(new WorkspaceFile
                {
                    FileName = safeName,
                    FullPath = fullPath
                });
            }
            catch (FormatException)
            {
                // Skip invalid Base64 without failing the entire preparation stage.
                continue;
            }
        }

        return result;
    }

    private static string BuildWorkspaceFolderName(string sourceIdentifier)
    {
        var id = SanitizeFileName(string.IsNullOrWhiteSpace(sourceIdentifier) ? "unknown" : sourceIdentifier);
        return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{id}";
    }

    private static string SanitizeFileName(string? name)
    {
        var baseName = string.IsNullOrWhiteSpace(name) ? "file" : Path.GetFileName(name);
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(baseName.Length);
        foreach (var ch in baseName)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }
        return sb.ToString();
    }

    private static string EnsureUniqueName(string directory, string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = fileName;
        var counter = 1;
        while (File.Exists(Path.Combine(directory, candidate)))
        {
            candidate = $"{name}({counter++}){ext}";
        }
        return candidate;
    }
}
