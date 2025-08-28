using System;
using System.Collections.Generic;
using System.IO;
using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.Sanitization;

public class SanitizationService : ISanitizationService
{
    // Basic rules: accept only PDF and images; discard likely email signatures/thumbs
    // and unsupported types. Heuristics only; no OCR at this stage.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"
    };

    // Common substrings for email signatures, icons, thumbnails, social logos, etc.
    private static readonly string[] SignatureOrThumbKeywords = new[]
    {
        "assinatura", "signature", "ass", "sig", "sign", "logo", "brand",
        "icon", "ico", "avatar", "thumb", "thumbnail", "mini", "small", "sm",
        "whatsapp", "facebook", "linkedin", "instagram", "twitter", "youtube"
    };

    private const long MinImageSizeBytes = 8 * 1024; // images below 8KB are likely non-informative

    public SanitizedFiles Process(PreparedWorkspace prepared)
    {
        if (prepared is null) throw new ArgumentNullException(nameof(prepared));

        var result = new SanitizedFiles
        {
            SourceIdentifier = string.Empty,
            WorkspaceFolderName = prepared.WorkspaceFolderName,
            WorkspaceFullPath = prepared.WorkspaceFullPath,
            AcceptedFiles = new(),
            DiscardedFiles = new()
        };

        if (prepared.Files is null || prepared.Files.Count == 0)
            return result;

        foreach (var file in prepared.Files)
        {
            var discarded = BuildDiscarded(file);
            if (discarded is not null)
                result.DiscardedFiles.Add(discarded);
            else
                result.AcceptedFiles.Add(BuildAccepted(file));
        }

        return result;
    }

    private SanitizedFile? BuildAccepted(WorkspaceFile wf)
    {
        var fullPath = wf.FullPath;
        var fileName = string.IsNullOrWhiteSpace(wf.FileName) ? Path.GetFileName(fullPath) : wf.FileName;
        var ext = Path.GetExtension(fullPath) ?? string.Empty;

        if (!AllowedExtensions.Contains(ext)) return null;

        var mime = GetMimeType(ext);

        if (ImageExtensions.Contains(ext))
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists) return null;
            if (IsSignatureOrThumbName(fileName)) return null;
            if (info.Length < MinImageSizeBytes) return null;
        }

        // PDF or accepted image
        return Accept(fileName, fullPath, mime);
    }

    private DiscardedFile? BuildDiscarded(WorkspaceFile wf)
    {
        if (wf is null) return new DiscardedFile { FileName = string.Empty, FullPath = string.Empty, Reason = "Null entry" };

        if (string.IsNullOrWhiteSpace(wf.FullPath))
        {
            var invalidName = string.IsNullOrWhiteSpace(wf.FileName) ? string.Empty : wf.FileName;
            return Discard(invalidName, wf.FullPath ?? string.Empty, "Invalid file path");
        }

        var fullPath = wf.FullPath;
        var fileName = string.IsNullOrWhiteSpace(wf.FileName) ? Path.GetFileName(fullPath) : wf.FileName;
        var ext = Path.GetExtension(fullPath) ?? string.Empty;

        if (!AllowedExtensions.Contains(ext))
            return Discard(fileName, fullPath, "Unsupported file type");

        if (ImageExtensions.Contains(ext))
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists)
                return Discard(fileName, fullPath, "File not found");

            if (IsSignatureOrThumbName(fileName))
                return Discard(fileName, fullPath, "Likely email signature/thumbnail");

            if (info.Length < MinImageSizeBytes)
                return Discard(fileName, fullPath, "Image too small (likely non-text)");
        }

        // PDF or accepted image -> not discarded
        return null;
    }

    private static SanitizedFile Accept(string name, string path, string? mime) => new()
    {
        FileName = name,
        FullPath = path,
        MimeType = mime
    };

    private static DiscardedFile Discard(string name, string path, string reason) => new()
    {
        FileName = name,
        FullPath = path,
        Reason = reason
    };

    private static bool IsSignatureOrThumbName(string fileName)
    {
        var name = fileName.ToLowerInvariant();
        foreach (var k in SignatureOrThumbKeywords)
        {
            if (name.Contains(k)) return true;
        }
        return false;
    }

    private static string? GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tif" or ".tiff" => "image/tiff",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => null
        };
    }
}
