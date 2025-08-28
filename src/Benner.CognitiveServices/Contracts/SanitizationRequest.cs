using System.Collections.Generic;

namespace Benner.CognitiveServices.Contracts;

public class SanitizationRequest
{
    public string SourceIdentifier { get; set; } = string.Empty;

    public List<InputFile> Files { get; set; } = new();
}

public class InputFile
{
    public string FileName { get; set; } = string.Empty;

    // File content in Base64 (PDF or image)
    public string FileContentBase64 { get; set; } = string.Empty;
}
