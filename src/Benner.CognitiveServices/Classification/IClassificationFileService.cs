using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.Classification;

public interface IClassificationFileService
{
    ClassificationResult Process(SanitizedFiles sanitized);
}
