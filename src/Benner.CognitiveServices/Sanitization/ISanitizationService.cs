using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.Sanitization;

public interface ISanitizationService
{
    SanitizedFiles Process(PreparedWorkspace prepared);
}
