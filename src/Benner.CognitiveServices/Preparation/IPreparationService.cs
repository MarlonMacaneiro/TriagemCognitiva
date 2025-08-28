using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.Preparation;

public interface IPreparationService
{
    PreparedWorkspace Prepare(SanitizationRequest request);
}
