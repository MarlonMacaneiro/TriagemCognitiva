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
            if (DetectBodyMail(text))
                type = ClassifiedFileType.Outros;
            else if (DetectBoleto(text))
                type = ClassifiedFileType.Boleto;
            else if (DetectNotaFiscal(text))
                type = ClassifiedFileType.NotaFiscal;
            else if ( DetectFaturaRecibo(text) )
                 type = ClassifiedFileType.FaturaRecibo;

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
        if (string.IsNullOrWhiteSpace(text)) return false;
        return DetectNotaFiscalMunicipal(text) || DetectNotaFiscalEstadual(text);
    }

    public bool DetectNotaFiscalMunicipal(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();

        if (!lower.Contains("prefeitura")) return false;
        bool hasNotaFiscalPhrase = lower.Contains("nota fiscal") || lower.Contains("nfs-e")
        || lower.Contains("nfs e") || lower.Contains("nfse")
        || lower.Contains("código de verificação") || lower.Contains("codigo de verificacao");

        bool hasTomador = lower.Contains("tomador");
        bool hasPrestador = lower.Contains("prestador");
        var cnpjRx_local = new System.Text.RegularExpressions.Regex("\\b\\d{2}\\.?\\d{3}\\.?\\d{3}/?\\d{4}-?\\d{2}\\b");
        var cnpjMatches_local = cnpjRx_local.Matches(text);

        int score = 0;
        if (hasTomador) score += 2;
        if (hasPrestador) score += 2;
        score += 2; // nota fiscal
        score += 2; // prefeitura

        string[] optionalTokens = {
            "chave de acesso", "inscrição municipal", "inscricao municipal",
            "iss", "serviço", "servicos", "serviço(s)", "regime especial tributação", "competência", "competencia",
            "discriminação dos serviços", "discriminacao dos servicos", "valor dos serviços", "valor total dos serviços"
        };
        int optionalHits = optionalTokens.Count(t => lower.Contains(t));
        score += optionalHits;

        var cnpjRegex = new System.Text.RegularExpressions.Regex("\\b\\d{2}\\.?\\d{3}\\.?\\d{3}/?\\d{4}-?\\d{2}\\b");
        var cnpjMatches = cnpjRegex.Matches(text);
        if (cnpjMatches.Count >= 2) score += 3; else if (cnpjMatches.Count == 1) score += 1;

        var longDigitSeq = System.Text.RegularExpressions.Regex.IsMatch(text, "\\b\\d{8,}\\b");
        if (longDigitSeq) score += 1;

        return score >= 8;
    }

    public bool DetectNotaFiscalEstadual(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();

        bool hasDanfe = lower.Contains("danfe") || lower.Contains("documento auxiliar");
        bool hasNfe = lower.Contains(" nfe") || lower.Contains(" nf-e") || lower.Contains(" nf e");
        bool hasChaveAcessoLabel = lower.Contains("chave de acesso") || lower.Contains("chave acesso");

        var chaveAcessoRegex = new System.Text.RegularExpressions.Regex("\\b\\d{44}\\b");
        bool hasChave44 = chaveAcessoRegex.IsMatch(text);

        var cnpjRegex = new System.Text.RegularExpressions.Regex("\\b\\d{2}\\.?\\d{3}\\.?\\d{3}/?\\d{4}-?\\d{2}\\b");
        var cnpjMatches = cnpjRegex.Matches(text);

        bool hasIE = lower.Contains("inscrição estadual") || lower.Contains("inscricao estadual") || lower.Contains("i.e.");

        int score = 0;
        if (hasDanfe)
            score += 3;
        if (hasNfe)
            score += 2;
        if (hasChaveAcessoLabel)
            score += 2;
        if (hasChave44)
            score += 3;
        if (cnpjMatches.Count >= 2)
            score += 2;
        else if (cnpjMatches.Count == 1)
            score += 1;
        if (hasIE)
            score += 1;

        return score >= 7;
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
                if (sameLineEmail || nextLineEmail) headerEmailHits++;
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
        System.Collections.Generic.List<string> keywords = new() { "ficha", "carteira", "autentic" };
        if (!keywords.All(k => lower.Contains(k))) return false;
        var wideNumeric = Regex.IsMatch(text, @"[\d\s\.\-]{45,}");
        if (wideNumeric)
        {
            var digits = text.Count(char.IsDigit);
            if (digits >= 40) return true;
        }
        return false;
    }

    public bool DetectFaturaRecibo(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();
        var fold = RemoveDiacritics(lower);

        bool hasRps = fold.Contains("rps");
        bool hasReciboProvisorio = fold.Contains("recibo provisorio de servico") || lower.Contains("recibo provisório de serviço");
        if (!(hasRps && hasReciboProvisorio)) return false;

        string[] keywords = new[]
        {
            // Hotelaria
            "hospedagem","hospede","diaria","hotel","quarto","reserva","check-in","check in","check-out","checkout",
            // Locação
            "locacao","locatario","locador","aluguel","aluguer","carro","veiculo",
            // EN
            "lodging","guest","room","hotel","reservation","checkin","checkout","daily rate","rate","night","nights","rental","vehicle","car"
        };

        int score = 0;
        foreach (var k in keywords)
        {
            var kFold = RemoveDiacritics(k.ToLowerInvariant());
            if (fold.Contains(kFold)) score += 1;
        }

        return score >= 5;
    }

    private static string RemoveDiacritics(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            switch (ch)
            {
                case 'á': case 'à': case 'ã': case 'â': sb.Append('a'); break;
                case 'é': case 'ê': sb.Append('e'); break;
                case 'í': sb.Append('i'); break;
                case 'ó': case 'ô': case 'õ': sb.Append('o'); break;
                case 'ú': sb.Append('u'); break;
                case 'ç': sb.Append('c'); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}
