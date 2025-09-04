using System;
using System.Linq;
using System.Text.RegularExpressions;
using Benner.CognitiveServices.Contracts;

namespace Benner.CognitiveServices.ClassificationType;

public class ClassificationFileType
{
    public ClassificationFileTypeResult Process(ExtractionContentFileResult extracted)
    {
        if (extracted is null) throw new ArgumentNullException(nameof(extracted));

        var result = new ClassificationFileTypeResult
        {
            SourceIdentifier = extracted.SourceIdentifier,
            WorkspaceFolderName = extracted.WorkspaceFolderName,
            WorkspaceFullPath = extracted.WorkspaceFullPath
        };

        if (extracted.Files is null || extracted.Files.Count == 0)
            return result;

        foreach (var f in extracted.Files)
        {
            var text = f.TextContent ?? string.Empty;
            var type = ClassifiedFileType.Outros;
            if ( DetectBodyMail(text) )
                type = ClassifiedFileType.Outros;
            else if (DetectBoleto(text))
                type = ClassifiedFileType.Boleto;
                
            // else if ( DetectNotaFiscal(text) )
            //     type = ClassifiedFileType.NotaFiscal;
            // else if ( DetectFaturaRecibo(text) )
            //     type = ClassifiedFileType.FaturaRecibo;

            result.Files.Add(new ClassificationFileTypeItem
            {
                FileName = f.FileName,
                FullPath = f.FullPath,
                TextContent = text,
                FileType = type
            });
        }

        return result;
    }

    public bool DetectBodyMail(string text)
    {
        // Heurística mais robusta para identificar corpo de e-mail.
        // Ideia: somar pontos para evidências típicas de e-mail e aplicar um threshold.
        // Mantido simples (sem NLP pesado) para performance em grandes volumes.
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Normalização básica
        var normalized = text.Replace('\r', '\n');
        while (normalized.Contains("\n\n\n")) normalized = normalized.Replace("\n\n\n", "\n\n");
        var lower = normalized.ToLowerInvariant();

        // Regex reutilizáveis (compiladas uma vez por AppDomain via cache local static)
        // Declaração local static garante inicialização uma vez e mantém o método self-contained.
        static Regex EmailRegex() => new(@"[a-z0-9_.+-]+@[a-z0-9-]+\.[a-z0-9-.]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex ReplySeparatorRegex() => new(@"^-{2,}\s*(mensagem original|original message|forwarded message)\s*-{2,}$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        // Cabeçalhos comuns (pt / en) que podem aparecer no topo ou em blocos de encaminhamento.
        var colonHeaderRegex = new Regex(@"^\s*(de|from|para|to|assunto|subject|cc|cco|bcc|data|sent|reply\-to)\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static bool StartsWithToken(string line, string token)
        {
            return line.StartsWith(token + " ", StringComparison.Ordinal) || line.StartsWith(token + "\t", StringComparison.Ordinal);
        }

        // Sinais de assinatura / formulações típicas.
        string[] signatureTokens =
        {
            "atenciosamente", "obrigado", "obrigada", "grato", "grata", "att.", "abs", "cordialmente", "sds.", "saudacoes", "cheers", "best regards", "regards"
        };

        // Palavras que aparecem com frequÃªncia em fluxos de email (anexo(s), responder, encaminhar etc.)
        string[] generalMailWords = { "anexo", "anexos", "responder", "resposta", "encaminhar", "encaminhado", "forward", "reply" };

        // EstratÃ©gia de pontuaÃ§Ã£o.
        int score = 0;

        // 1. Contar linhas de cabeçalho estruturadas (antes de um bloco vazio) ou em bloco de encaminhamento.
        var lines = normalized.Split('\n');
        int headerLikeCount = 0;
        foreach (var rawLine in lines.Take(60))
        {
            var line = rawLine.Trim();
            if (colonHeaderRegex.IsMatch(line)
                || StartsWithToken(line, "De")
                || StartsWithToken(line, "Para")
                || StartsWithToken(line, "Cc")
                || StartsWithToken(line, "Assunto")
                || StartsWithToken(line, "Data")
                || StartsWithToken(line, "From")
                || StartsWithToken(line, "To")
                || StartsWithToken(line, "Subject"))
            {
                headerLikeCount++;
            }
        }
        if (headerLikeCount >= 2) score += 3; // cabeçalhos fortes
        else if (headerLikeCount == 1) score += 1; // fraco mas conta

        // 2. Emails explícitos.
        var hasEmailAddress = EmailRegex().IsMatch(text);
        if (hasEmailAddress) score += 2;

        // 3. Separadores de resposta / encaminhamento.
        var hasReplySep = ReplySeparatorRegex().IsMatch(text); if (hasReplySep) score += 2;

        // 4. Tokens gerais de email.
        int generalHits = generalMailWords.Count(w => lower.Contains(w));
        if (generalHits >= 2) score += 2; else if (generalHits == 1) score += 1;

        // 5. Assinatura provável (token de assinatura no último terço do texto)
        var lastThirdIndex = (int)(lines.Length * 0.66);
        var tail = string.Join('\n', lines.Skip(lastThirdIndex));
        if (signatureTokens.Any(t => tail.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) score += 1;

        // 6. Comprimento característico (evita marcar textos muito curtos sem contexto)
        if (text.Length > 120) score += 1; // pequeno bÃ´nus

        // Threshold ajustado empiricamente: >=4 indica forte evidÃªncia de corpo de e-mail.
        if (headerLikeCount == 0 && !hasReplySep) return false; return score >= 5;
    }


    public bool DetectBoleto(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();

        // Palavras-chave comuns
        List<string> keywords = new() {"ficha", "carteira", "autentic"};

        if (!keywords.All(k => lower.Contains(k))) return false;

        // Padrão específico para código de barras do boleto (47 ou 48 dígitos)
        var wideNumeric = Regex.IsMatch(text, @"[\d\s\.\-]{45,}");
        if (wideNumeric)
        {
            var digits = text.Count(char.IsDigit);
            if (digits >= 40) return true;
        }

        return false;
    }
}






