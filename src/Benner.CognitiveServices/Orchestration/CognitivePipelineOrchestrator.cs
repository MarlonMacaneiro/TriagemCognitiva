using Benner.CognitiveServices.Contracts;
using Benner.CognitiveServices.Sanitization;
using Benner.CognitiveServices.Preparation;
using Benner.CognitiveServices.Classification;

namespace Benner.CognitiveServices.Orchestration;

public class CognitivePipelineOrchestrator
{
    private readonly ISanitizationService _sanitizationService;
    private readonly IPreparationService _preparationService;
    private readonly IClassificationFileService _classificationFileService;

    public CognitivePipelineOrchestrator(IPreparationService preparationService, ISanitizationService sanitizationService, IClassificationFileService classificationFileService)
    {
        _preparationService = preparationService;
        _sanitizationService = sanitizationService;
        _classificationFileService = classificationFileService;
    }

    public void Run(SanitizationRequest request)
    {
        // 1) Prepare workspace
        var prepared = _preparationService.Prepare(request);

        // 2) Sanitization
        var sanitized = _sanitizationService.Process(prepared);

        // 3) Classification (types of files)
        var classified = _classificationFileService.Process(sanitized);

        // Next steps (to be added as services are implemented):
        // - Triage (country/layout)
        // - Info classification & prompt selection
        // - Prompt processing via API
        // - Result handling and serialization
    }
}
