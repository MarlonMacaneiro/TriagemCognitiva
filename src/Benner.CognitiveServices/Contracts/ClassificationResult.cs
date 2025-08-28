using System.Collections.Generic;

namespace Benner.CognitiveServices.Contracts;

public class ClassificationResult
{
    public string SourceIdentifier { get; set; } = string.Empty;
    public string WorkspaceFolderName { get; set; } = string.Empty;
    public string WorkspaceFullPath { get; set; } = string.Empty;

    public List<ClassifiedFile> Files { get; set; } = new();
}

public class ClassifiedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    // Tipo vindo do SanitizedFiles (usaremos o MIME quando disponível)
    public string FileType { get; set; } = string.Empty;
    // Conteúdo textual extraído (PDF com texto ou via OCR)
    public string? TextContent { get; set; }
}

