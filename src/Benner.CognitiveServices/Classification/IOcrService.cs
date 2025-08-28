namespace Benner.CognitiveServices.Classification;

public interface IOcrService
{
    // Lê o conteúdo textual de um arquivo (imagem ou PDF) via OCR
    string ReadText(string filePath);
}

