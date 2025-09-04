using Benner.CognitiveServices.Contracts;
using Benner.CognitiveServices.Sanitization;
using Benner.CognitiveServices.Preparation;
using Benner.CognitiveServices.ExtractionContent;
using Benner.CognitiveServices.ClassificationType;

namespace Benner.CognitiveServices.Orchestration;

public class CognitivePipelineOrchestrator
{
    private readonly ISanitizationService _sanitizationService;
    private readonly IPreparationService _preparationService;
    private readonly IExtractionContentFileService _extractionContentFileService;
    private readonly ClassificationFileType _classificationFileType;

    public CognitivePipelineOrchestrator(IPreparationService preparationService, ISanitizationService sanitizationService, IExtractionContentFileService extractionContentFileService, ClassificationFileType classificationFileType)
    {
        _preparationService = preparationService;
        _sanitizationService = sanitizationService;
        _extractionContentFileService = extractionContentFileService;
        _classificationFileType = classificationFileType;
    }

    public void Run(SanitizationRequest request)
    {
        // 1) Prepare workspace
        var prepared = _preparationService.Prepare(request);

        // 2) Sanitization
        var sanitized = _sanitizationService.Process(prepared);

        // 3) Extraction content (text from files)
        var extractionFileContents = _extractionContentFileService.Process(sanitized);

        // 4) Classification type of file (Boleto, NotaFiscal, FaturaRecibo, Outros)
        var typed = _classificationFileType.Process(extractionFileContents);

        // Next steps (to be added as services are implemented):
        // - Classification type of file ( FaturaRecibo, Boleto, NotaFiscal, Outros )
        // - Triage (country/layout)
        // - Info classification & prompt selection
        // - Prompt processing via API
        // - Result handling and serialization
    }
}
