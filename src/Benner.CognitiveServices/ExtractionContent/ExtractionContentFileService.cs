using System;
using System.IO;
using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.ExtractionContent;

public class ExtractionContentFileService : IExtractionContentFileService
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IOcrService _ocrService;

    public ExtractionContentFileService(IPdfTextExtractor pdfExtractor, IOcrService ocrService)
    {
        _pdfExtractor = pdfExtractor ?? throw new ArgumentNullException(nameof(pdfExtractor));
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
    }

    public ExtractionContentFileResult Process(SanitizedFiles sanitized)
    {
        if (sanitized is null) throw new ArgumentNullException(nameof(sanitized));

        var result = new ExtractionContentFileResult
        {
            SourceIdentifier = sanitized.SourceIdentifier,
            WorkspaceFolderName = sanitized.WorkspaceFolderName,
            WorkspaceFullPath = sanitized.WorkspaceFullPath
        };

        if (sanitized.AcceptedFiles is null || sanitized.AcceptedFiles.Count == 0)
            return result;

        foreach (var file in sanitized.AcceptedFiles)
        {
            var fileContentExtraction  = new FileContentExtraction
            {
                FileName = file.FileName,
                FullPath = file.FullPath,
                FileType = file.MimeType
            };

            var isPdf = IsPdf(file.MimeType, file.FullPath);

            string? textContent = null;

            if (isPdf)
                // Tentar extrair texto embutido do PDF
                textContent = SafeExtractPdfText(file.FullPath);

            if (string.IsNullOrWhiteSpace(textContent))
                // Imagem (ou outro) -> OCR
                textContent = SafeOcr(file.FullPath);

            fileContentExtraction.TextContent = textContent;
            result.Files.Add(fileContentExtraction);
        }

        return result;
    }

    private string? SafeExtractPdfText(string path)
    {
        try { return _pdfExtractor.ExtractText(path); }
        catch { return null; }
    }

    private string? SafeOcr(string path)
    {
        try { return _ocrService.ReadText(path); }
        catch { return null; }
    }

    private static bool IsPdf(string? mimeOrNull, string path)
    {
        if (!string.IsNullOrWhiteSpace(mimeOrNull))
            return string.Equals(mimeOrNull, "application/pdf", StringComparison.OrdinalIgnoreCase);

        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
