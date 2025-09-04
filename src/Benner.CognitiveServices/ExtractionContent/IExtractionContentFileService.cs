using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.ExtractionContent;

public interface IExtractionContentFileService
{
    ExtractionContentFileResult Process(SanitizedFiles sanitized);
}
