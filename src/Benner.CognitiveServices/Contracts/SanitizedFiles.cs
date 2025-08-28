using System.Collections.Generic;

namespace Benner.CognitiveServices.Contracts;

public class SanitizedFiles
{
    public string SourceIdentifier { get; set; } = string.Empty;
    public string WorkspaceFolderName { get; set; } = string.Empty;
    public string WorkspaceFullPath { get; set; } = string.Empty;

    public List<SanitizedFile> AcceptedFiles { get; set; } = new();
    public List<DiscardedFile> DiscardedFiles { get; set; } = new();
}

public class SanitizedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

public class DiscardedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty; // to be filled when rules are implemented
}
