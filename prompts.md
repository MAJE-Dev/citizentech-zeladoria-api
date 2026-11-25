# Uso de IA — prompts, finalidade e validação

**Houve uso de IA: sim.** A ferramenta foi o Claude Code (Anthropic), modelo Claude Opus, usado em 25/11/2026 na máquina do integrante Vitor Rivas Cardoso (VitorCardoso_RM556404) para refazer a entrega depois do feedback do professor. A IA **não** faz parte da aplicação: a API não chama nenhum serviço de IA nem serviço pago.

## Prompts relevantes

Os textos abaixo estão resumidos. Removemos a linguagem informal e mantivemos o conteúdo do pedido.

| # | Prompt (resumo) | Finalidade | Resultado aproveitado | Validação prática pela equipe |
|---|---|---|---|---|
| 1 | "O professor disse que a entrega não seguiu a rubrica. Verifique tudo (documentos D02/D03/D04, ZIP enviado, repositório GitHub e o print do professor) e refaça para atender a rubrica." | Auditar a entrega anterior contra a rubrica | Lista de lacunas por critério: banco InMemory (sem DDL/SQL), imagem salva como caminho em `wwwroot/uploads`, transação ignorada pelo InMemory (`TransactionIgnoredWarning`), ZIP sem README/prompts/modelo/sql/testes/docs/evidências e com `.vs/` | Conferido pela equipe nos PDFs D02/D04 e no código antigo |
| 2 | Escolha de banco e ambiente: "instale o necessário para gerar evidências reais; use SQL Server LocalDB". | Ter execução real, não só código | .NET SDK 10.0.404 e SQL Server 2022 Express LocalDB instalados via winget/instalador oficial | `E02_01`, `E02_02` (versões reais coletadas na máquina) |
| 3 | Reestruturar o domínio (zeladoria urbana) em 5 tabelas no padrão do D02, com imagem em bytes e uma transação com duas escritas | Atender R3, R5 e R6 | DDL `01_Criar_Estrutura.sql`, carga, verificação, `AppDb.cs`, controllers, operação "registrar atendimento" (UPDATE do pendente + INSERT do atendimento) | Scripts executados (`E03_01`–`E03_03`), build sem avisos e 11 testes unitários aprovados (`E02_03`) |
| 4 | Gerar cliente HTTP em C# e o roteiro automático T01–T10 com antes/depois em SQL | Atender R4, R7 e R8 com evidências rastreáveis | `CitizenTech.Zeladoria.Cliente`, `testes/Executar-Roteiro-T01-T10.ps1` | Roteiro executado contra a API real: a 1ª execução achou 2 divergências (LINQ não traduzível, `E09_02`), que foram corrigidas; a 2ª teve **54/54 verificações** (`E08_00`) |
| 5 | Gerar o modelo editável (diagrams.net), DBML, dicionário, README, índice e relatório D03 com valores observados | Atender R2, R3 e R9 | Arquivos em `modelo/`, `README.md`, `evidencias/indice.md`, `docs/relatorio.pdf` | Modelo aberto no diagrams.net (`E03_04`). Valores do relatório copiados dos arquivos de evidência |
| 6 | Imagem de teste | Ter imagem pertinente com origem conhecida | Ilustração própria gerada por script (`amostras/Gerar-Amostras.ps1`), sem direitos de terceiros. A imagem anterior (um meme) foi descartada | Arquivo gerado localmente, com o hash registrado em `E04_02` |

## O que a IA não fez

- **Não inventou a escuta:** a conversa de 14/11/2026 na Mooca foi feita e registrada pelo integrante Vitor. A IA apenas organizou o registro (`E01_01`).
- **Não marcou testes como aprovados sem execução:** toda situação "aprovado" vem de saída real gravada em `evidencias/`.
- **Não gerou dados acadêmicos:** nomes e RMs são reais e foram informados pela equipe. Os dados de domínio (endereços, ocorrências) são fictícios e estão declarados assim.

## Fontes

- Documentos da disciplina: D02 (tutorial prático), D03 (modelo de relatório) e D04 (rubrica), versão 05, de 01/11/2026.
- Documentação Microsoft: ASP.NET Core Web API, EF Core (`ExecuteUpdateAsync`, transações), System.Text.Json.
- Ícone ODS 11: Nações Unidas / ONU Brasil (ver `amostras/CREDITOS.md`).
