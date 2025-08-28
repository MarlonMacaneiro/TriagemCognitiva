using System.Collections.Generic;

namespace Benner.CognitiveServices.Contracts;

public class PreparedWorkspace
{
    public string WorkspaceFolderName { get; set; } = string.Empty;
    public string WorkspaceFullPath { get; set; } = string.Empty;
    public List<WorkspaceFile> Files { get; set; } = new();
}

public class WorkspaceFile
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
}
