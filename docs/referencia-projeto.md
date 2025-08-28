# Benner Cognitive Services (BCS) — Triagem Cognitiva de Arquivos

Documento de referência do serviço BCS (Benner Cognitive Services).
Tagline: IA cognitiva para triagem e extração documental.

## Visão Geral
- Produto: Benner Cognitive Services (BCS) — serviço cognitivo de IA hospedado.
- Objetivo: classificar, extrair e importar informações de documentos (PDF e imagens) relacionados a reservas hoteleiras, com apoio de IA.
- Escopo: sanitização de anexos, triagem por tipo/país/layout, definição de prompt, processamento via API, revisão do usuário e importação final.

## Pipeline de Processamento

### 1) Sanitização
- Separar os arquivos válidos dos inválidos para leitura.
- Descartar arquivos que não sejam PDF ou imagem.

### 2) Triagem — Classificação dos Tipos de Arquivo
- Mapear tipos: Nota Fiscal, Fatura, Boleto.
- Regras textuais rápidas (se PDF pesquisável ou após OCR raso da 1ª página):
  - Palavras-chave/sinônimos por idioma (exemplos/regex):
    - Hospede|Hóspede|Guest
    - Check[-\s]?in, Check[-\s]?out
    - Tarifa|Diárias|Room\sRate|Fare
    - Taxa|ISS|VAT|City\sTax
    - NF-e|NFe|Nota\s*Fiscal (Brasil)
    - Invoice (EN)
- Campos país-específicos (forte indicativo BR quando presentes):
  - CNPJ, CPF, Inscrição Estadual (a confirmar)
- Se não for reconhecido como documento de hotel: decidir descarte.
- Exemplo: se o anexo for um e-mail e for o único anexo, avaliar extrair diretamente do corpo as informações de check-in, check-out, hóspede e valores da reserva.

### 3) Triagem — Classificação do País
- Classificar documento como nacional ou exterior.

### 4) Triagem — Classificação do Layout
- Haverá um conjunto de layouts mapeados para maior precisão e menor custo de processamento.

### 5) Classificador de Informações
- Verificar se o conteúdo do arquivo é aderente ao prompt (se possui informações da reserva).
- Marcar os arquivos que serão enviados ao prompt.
- Definir o prompt do arquivo conforme categoria:
  - Nacional genérico
  - Internacional genérico
  - Accor
  - Atlântica
  - Intercity

### 6) Processar Prompt
- Ler informações categorizadas do PDF e apresentar no formato:
  - CONFIRMACAO:
  - TOTALDIARIAS:
  - TOTALTAXAS:
  - CHECKIN:
  - CHECKOUT:
  - CNPJFORNECEDOR:
- Junto com cada informação, indicar a página de onde foi extraída.
- Ao final, descrever como cada informação foi obtida de forma amigável, para depuração do usuário final.

### 7) Tratar Retorno
- Serializar as informações retornadas pelo prompt em um contrato de API.

## Sistema (Integração e UX)

### Comando
- Criar comando: “Processar com IA”.

### Processamento
- Processamento ocorrerá em BTL, consumindo API desenvolvida e executada em um serviço Docker hospedado pela Benner.

### Acompanhamento do Processamento
- Exibir status atual.
- Indicar erros, quando houver.

### Exibição do Resultado
- Mostrar os dados retornados pela API.
- Mostrar como serão importados no sistema.
- Exibir detalhamento do fluxo de extração dos dados pela IA.

### Ações do Usuário
- Visualizar os dados por linha (venda).
- Ajustar dados das linhas.
- Confirmar as linhas que serão importadas.

### Importação dos Dados
- Importar os dados para uma fatura com seus detalhes.

## Observações e Pontos em Aberto
- Confirmar campos país-específicos (CNPJ, CPF, Inscrição Estadual) e regras de detecção.
- Definir critérios objetivos de descarte para documentos não reconhecidos como hotel.
- Especificar contratos de API (request/response) e versionamento.
- Definir estratégia de OCR (biblioteca, idiomas, custo/desempenho) e fallback quando não pesquisável.
- Mapear layouts conhecidos (Accor, Atlântica, Intercity, etc.) e critérios de seleção.

## Próximos Passos (rascunho)
- Detalhar contrato da API e formatos de saída.
- Especificar prompts por cenário (nacional, internacional e redes específicas).
- Definir heurísticas e limiares de confiança para triagem e descarte.
- Prototipar fluxo end-to-end em ambiente de desenvolvimento.
