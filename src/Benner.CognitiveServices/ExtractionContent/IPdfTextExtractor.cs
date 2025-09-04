namespace Benner.CognitiveServices.ExtractionContent;

public interface IPdfTextExtractor
{
    // Extrai texto de um PDF sem OCR (texto embutido)
    string ExtractText(string filePath);
}
