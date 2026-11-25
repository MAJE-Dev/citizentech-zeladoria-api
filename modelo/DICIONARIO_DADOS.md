# Dicionário de dados — Zeladoria API Urbana (SQL Server)

GS 2026.2 · C# Software Development · Turma 3ESPH · Líder: Kaio Drago Lima Souza (KaioSouza_RM556095)

- **Banco escolhido:** Microsoft SQL Server. Na execução da equipe, usamos o SQL Server 2022 Express LocalDB 16.0.1000.6, no banco `GS_CSHARP_ZELADORIA`.
- **Fonte física oficial:** `sql/sqlserver/01_Criar_Estrutura.sql`. A API não cria tabelas.
- **Mapeamento C#:** `src/CitizenTech.Zeladoria.Api/Dados/AppDb.cs`. Cada propriedade vira uma coluna com o nome em MAIÚSCULAS, e `DateTime` vira `datetime2(6)` lido como UTC.
- **Modelo editável:** `modelo/Zeladoria_Modelo.drawio` (diagrams.net). Exportação: `modelo/Zeladoria_Modelo.png`. Alternativa lógica: `modelo/Zeladoria_Modelo.dbml`.
- **Convenções:**
  - IDs `int IDENTITY(100,1)`. A carga fictícia usa IDs fixos de 1 a 3, e os registros novos começam em 100. Pode haver lacunas de ID depois de um rollback.
  - Todas as FKs são obrigatórias no lado filho, com `ON DELETE NO ACTION` (exclusão restrita). Cardinalidade: 1 pai para 0..N filhos.

## Visão geral

| Tabela | Responsabilidade no domínio | PK | FKs | Relações |
|---|---|---|---|---|
| REGIAO | Território (subprefeitura/bairro) | ID | — | 1 região → 0..N ocorrências |
| CATEGORIA_OCORRENCIA | Tipo do problema urbano | ID | — | 1 categoria → 0..N ocorrências |
| OCORRENCIA | Problema relatado pelo cidadão, com quantidade de serviço pendente ("saldo") | ID | REGIAOID, CATEGORIAID | N:1 região, N:1 categoria; 1 → 0..N imagens e 0..N atendimentos |
| IMAGEM_OCORRENCIA | Foto do problema em **bytes** e metadados técnicos | ID | OCORRENCIAID | N:1 ocorrência |
| ATENDIMENTO_OCORRENCIA | Serviço executado em campo, que baixa o pendente | ID | OCORRENCIAID | N:1 ocorrência |

## REGIAO

| Coluna | Tipo SQL Server | Nulo | Regra / descrição | Propriedade C# |
|---|---|---|---|---|
| ID | int IDENTITY(100,1) | não | PK `PK_REGIAO` | `Regiao.Id` |
| NOME | nvarchar(100) | não | Nome da subprefeitura | `Nome` |
| BAIRRO | nvarchar(80) | não | Bairro de referência | `Bairro` |
| CIDADE | nvarchar(80) | não | Município | `Cidade` |

## CATEGORIA_OCORRENCIA

| Coluna | Tipo | Nulo | Regra / descrição | Propriedade C# |
|---|---|---|---|---|
| ID | int IDENTITY(100,1) | não | PK `PK_CATEGORIA_OCORRENCIA` | `CategoriaOcorrencia.Id` |
| NOME | nvarchar(60) | não | `UQ_CATEGORIA_OCORRENCIA_NOME` (único) | `Nome` |

## OCORRENCIA

| Coluna | Tipo | Nulo | Regra / descrição | Propriedade C# |
|---|---|---|---|---|
| ID | int IDENTITY(100,1) | não | PK `PK_OCORRENCIA` | `Ocorrencia.Id` |
| REGIAOID | int | não | FK `FK_OCORRENCIA_REGIAO` → REGIAO(ID) | `RegiaoId` |
| CATEGORIAID | int | não | FK `FK_OCORRENCIA_CATEGORIA` → CATEGORIA_OCORRENCIA(ID) | `CategoriaId` |
| TITULO | nvarchar(100) | não | Resumo do problema (API: obrigatório, até 100) | `Titulo` |
| DESCRICAO | nvarchar(500) | não | Relato (API: obrigatório, até 500) | `Descricao` |
| ENDERECO | nvarchar(150) | não | Local (dados fictícios na carga) | `Endereco` |
| UNIDADE | nvarchar(10) | não | Unidade do serviço: m2, m3, m, un, kg (validada na API) | `Unidade` |
| QUANTIDADEESTIMADA | decimal(12,3) | não | `CK_OCORRENCIA_ESTIMADA`: > 0. Total de serviço estimado na abertura | `QuantidadeEstimada` |
| QUANTIDADEPENDENTE | decimal(12,3) | não | `CK_OCORRENCIA_PENDENTE`: 0 ≤ pendente ≤ estimada. É o **saldo** baixado pela transação | `QuantidadePendente` |
| SITUACAO | nvarchar(20) | não | `CK_OCORRENCIA_SITUACAO`: ABERTA, EM_ATENDIMENTO ou CONCLUIDA. `CK_OCORRENCIA_CONCLUSAO`: CONCLUIDA ⇔ pendente = 0 | `Situacao` |
| ABERTAUTC | datetime2(6) | não | Data/hora UTC do registro | `AbertaUtc` |

## IMAGEM_OCORRENCIA

| Coluna | Tipo | Nulo | Regra / descrição | Propriedade C# |
|---|---|---|---|---|
| ID | int IDENTITY(100,1) | não | PK `PK_IMAGEM_OCORRENCIA`. É devolvido pela API só depois do `SaveChangesAsync` | `ImagemOcorrencia.Id` |
| OCORRENCIAID | int | não | FK `FK_IMAGEM_OCORRENCIA` → OCORRENCIA(ID). Vínculo com o domínio | `OcorrenciaId` |
| TITULO | nvarchar(100) | não | Título informado no upload | `Titulo` |
| NOMEARQUIVO | nvarchar(200) | não | Nome original sem o caminho do cliente | `NomeArquivo` |
| MIME | nvarchar(30) | não | `CK_IMAGEM_MIME`: image/png ou image/jpeg, conforme a assinatura real | `Mime` |
| TAMANHOBYTES | int | não | `CK_IMAGEM_TAMANHO`: entre 1 e 2097152 e igual a `DATALENGTH(DADOS)` | `TamanhoBytes` |
| HASHSHA256 | char(64) | não | SHA-256 em hexadecimal maiúsculo, calculado pela API e conferível com `HASHBYTES('SHA2_256', DADOS)` | `HashSha256` |
| DADOS | varbinary(max) | não | **Bytes reais da imagem** (não é caminho de arquivo) | `Dados` (`byte[]`) |
| EQUIPERM | nvarchar(20) | não | RM do líder (`equipe.json`) | `EquipeRm` |
| EXECUTOR | nvarchar(80) | não | NomeAluno_RM de quem enviou (validado contra a equipe) | `Executor` |
| ENVIADAUTC | datetime2(6) | não | Data/hora UTC do upload | `EnviadaUtc` |

## ATENDIMENTO_OCORRENCIA

| Coluna | Tipo | Nulo | Regra / descrição | Propriedade C# |
|---|---|---|---|---|
| ID | int IDENTITY(100,1) | não | PK `PK_ATENDIMENTO_OCORRENCIA` | `AtendimentoOcorrencia.Id` |
| OCORRENCIAID | int | não | FK `FK_ATENDIMENTO_OCORRENCIA` → OCORRENCIA(ID) | `OcorrenciaId` |
| QUANTIDADE | decimal(12,3) | não | `CK_ATENDIMENTO_QTD`: > 0. A API também limita a 3 casas decimais | `Quantidade` |
| EQUIPECAMPOCODIGO | nvarchar(40) | não | Código fictício da equipe de zeladoria (ex.: EQ_ZEL_MOOCA_01) | `EquipeCampoCodigo` |
| EQUIPERM | nvarchar(20) | não | RM do líder | `EquipeRm` |
| EXECUTOR | nvarchar(80) | não | NomeAluno_RM de quem registrou | `Executor` |
| CRIADOUTC | datetime2(6) | não | Data/hora UTC | `CriadoUtc` |

## Regra de consistência verificada em SQL

`QUANTIDADEESTIMADA = QUANTIDADEPENDENTE + SUM(ATENDIMENTO_OCORRENCIA.QUANTIDADE)` para cada ocorrência. A coluna `CONSISTENTE` do `03_Verificar_Resultados.sql` confere essa regra. Depois de um ROLLBACK, a regra continua valendo porque nem o UPDATE nem o INSERT ficam no banco.

## Diferenças entre o modelo lógico e o físico

- O DBML e o diagrama mostram tipos de referência do SQL Server. Não há versão Oracle: escolhemos um único banco, o que a rubrica permite ("um banco basta").
- Índices não únicos sobre as FKs (`IX_*`) existem apenas no físico e servem às consultas por vínculo.
- As constraints CHECK (`CK_*`) estão no DDL. A API repete as validações de entrada para responder 4xx com uma mensagem clara antes de tentar gravar.
- A identificação da equipe (integrantes, RM, líder) **não** é uma tabela. Ela fica em `src/CitizenTech.Zeladoria.Api/equipe.json` e nos metadados EQUIPERM/EXECUTOR, conforme o D02 §6.6.
