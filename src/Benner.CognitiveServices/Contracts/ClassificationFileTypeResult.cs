using System.Collections.Generic;

namespace Benner.CognitiveServices.Contracts;

public class ClassificationFileTypeResult
{
    public string SourceIdentifier { get; set; } = string.Empty;
    public string WorkspaceFolderName { get; set; } = string.Empty;
    public string WorkspaceFullPath { get; set; } = string.Empty;

    public List<ClassificationFileTypeItem> Files { get; set; } = new();
}

public class ClassificationFileTypeItem
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public ClassifiedFileType FileType { get; set; } = ClassifiedFileType.Outros;
}

public enum ClassifiedFileType
{
    FaturaRecibo,
    Boleto,
    NotaFiscal,
    Outros
}

