namespace Benner.CognitiveServices.Classification;

public interface IPdfTextExtractor
{
    // Extrai texto de um PDF sem OCR (texto embutido)
    string ExtractText(string filePath);
}

