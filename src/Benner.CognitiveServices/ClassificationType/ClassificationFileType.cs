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
            else if (DetectNotaFiscal(text))
                type = ClassifiedFileType.NotaFiscal;


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

    public bool DetectNotaFiscal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var lower = text.ToLowerInvariant();

        // Requisitos mínimos para considerar: presença de 'prefeitura' e ( 'nota fiscal' ou 'nfs-e' ) e ao menos um de tomador/prestador.
        if (!lower.Contains("prefeitura"))
            return false;
            
        bool hasNotaFiscalPhrase =
            lower.Contains("nota fiscal") || lower.Contains("nfs-e")
            || lower.Contains("nfs e")
            || lower.Contains("nfse")

            || lower.Contains("nf-e")
            || lower.Contains("nf e")
            || lower.Contains("nfe")

            || lower.Contains("código de verificação")
            || lower.Contains("codigo de verificacao");

        if (!hasNotaFiscalPhrase)
            return false;
        bool hasTomador = lower.Contains("tomador");
        bool hasPrestador = lower.Contains("prestador");

        // Heurística de pontuação para fortalecer a detecção e reduzir falso positivo.
        int score = 0;

        // Elementos principais
        if (hasTomador) score += 2;
        if (hasPrestador) score += 2;
        score += 2; // nota fiscal (já validado)
        score += 2; // prefeitura (já validado)

        // Campos típicos de NFS-e municipal
        string[] optionalTokens =
        {
            "chave de acesso", "inscrição municipal", "inscricao municipal",
            "iss", "serviço", "servicos", "serviço(s)", "regime especial tributação", "competência", "competencia",
            "discriminação dos serviços", "discriminacao dos servicos", "valor dos serviços", "valor total dos serviços"
        };
        int optionalHits = optionalTokens.Count(t => lower.Contains(t));
        score += optionalHits; // 1 ponto por token encontrado

        // Padrões de CNPJ: presença de dois CNPJs (prestador e tomador) reforça muito.
        var cnpjRegex = new Regex(@"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b");
        var cnpjMatches = cnpjRegex.Matches(text);
        if (cnpjMatches.Count >= 2) score += 3; else if (cnpjMatches.Count == 1) score += 1;

        // Código de verificação / chave longa numérica (>= 30 dígitos espalhados) também reforça.
        var longDigitSeq = Regex.IsMatch(text, @"\b\d{8,}\b");
        if (longDigitSeq) score += 1;

        // Threshold empírico: >=8 sugere forte evidência de NFS-e.
        return score >= 8;
    }

    public bool DetectBodyMail(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = text.Replace('\r', '\n');
        while (normalized.Contains("\n\n\n")) normalized = normalized.Replace("\n\n\n", "\n\n");
        var lower = normalized.ToLowerInvariant();

        static Regex EmailRegex() => new(@"[a-z0-9_.+-]+@[a-z0-9-]+\.[a-z0-9-.]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var headers = new[] { "de", "para", "from", "to", "cc", "cco", "bcc", "assunto", "subject", "data", "sent", "reply-to" };
        var bodyPhrases = new[] { "segue em anexo", "em anexo", "anexo", "bom dia", "boa tarde", "boa noite", "favor ", "gentileza", "segue abaixo" };

        int score = 0;
        var emailRx = EmailRegex();
        var lines = normalized.Split('\n');

        int headerEmailHits = 0;
        for (int i = 0; i < Math.Min(lines.Length, 120); i++)
        {
            var line = lines[i].Trim();
            foreach (var h in headers)
            {
                bool isHeader = line.StartsWith(h + ":", StringComparison.OrdinalIgnoreCase)
                                || line.StartsWith(h + " ", StringComparison.OrdinalIgnoreCase)
                                || line.StartsWith(h + "\t", StringComparison.OrdinalIgnoreCase);
                if (!isHeader) continue;

                bool sameLineEmail = emailRx.IsMatch(line);
                bool nextLineEmail = false;
                for (int j = i + 1; j < Math.Min(lines.Length, i + 3); j++)
                {
                    var ln = lines[j].Trim();
                    if (ln.Length == 0) continue;
                    nextLineEmail = emailRx.IsMatch(ln);
                    break;
                }
                if (sameLineEmail || nextLineEmail)
                    headerEmailHits++;
            }
        }

        if (headerEmailHits > 0) score += 4;
        if (bodyPhrases.Any(p => lower.Contains(p))) score += 1;

        return score >= 5;
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








