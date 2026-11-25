# Zeladoria API Urbana — GS 2026.2 · C# Software Development · 3ESPH

Professor: Angel Antonio Gonzalez Martinez · Entrega: `GS_CSHARP_KaioSouza_RM556095.zip` (líder Kaio Drago Lima Souza, RM556095)

## Ficha da CitizenTech

| Campo | Detalhe |
|---|---|
| **CitizenTech** | Zeladoria API Urbana (repositório `citizentech-zeladoria-api`) |
| **Cidade e recorte** | São Paulo/SP. O recorte piloto é a **Mooca** (Subprefeitura Mooca), onde fizemos a escuta. A carga também tem Vila Mariana. |
| **Cidadão beneficiado** | Pedestres, moradores e usuários de transporte público que dependem de vias e calçadas seguras, principalmente no trajeto a pé até o ponto de ônibus à noite. |
| **Problema, em uma frase** | Buracos e postes apagados demoram meses para ser resolvidos, e quem reclama não vê o andamento ("só dizem que está em análise"). |
| **Como funciona** | Uma Web API ASP.NET Core (.NET 10) com SQL Server. O cidadão registra a **ocorrência** e anexa a **foto do problema**, que é guardada em **bytes** no banco. A equipe de campo registra **atendimentos**: cada atendimento dá baixa na quantidade pendente (m², pontos de luz…) e grava o histórico **na mesma transação** (COMMIT/ROLLBACK). Qualquer pessoa consulta a situação, o pendente e o histórico por HTTP/JSON. |
| **O que cada parte faz** | `src/CitizenTech.Zeladoria.Api`: controllers REST, validações, EF Core/LINQ, upload por stream, transação. `sql/sqlserver`: DDL, carga fictícia, verificação e demo de transação. `src/CitizenTech.Zeladoria.Cliente`: cliente HTTP em C# (HttpClient, multipart, streams, JSON) que consome a mesma API. `src/CitizenTech.Zeladoria.Tests`: testes unitários das validações. `testes/`: roteiro real T01–T10 e `api.http`. `modelo/`: diagrama editável e dicionário. |
| **Observação e escuta** | Feita em 14/11/2026 (~14h30), na Mooca, por Vitor Rivas Cardoso, com um morador que passa pela rua todos os dias (anônimo). Ele relatou um buraco fundo há meses que enche d'água, metade dos postes apagados depois das 19h e dois protocolos abertos sem retorno. Registro completo: [`evidencias/E01_01_Registro_Escuta_Mooca_2026-11-14.md`](evidencias/E01_01_Registro_Escuta_Mooca_2026-11-14.md). |
| **ODS** | **ODS 11, Cidades e Comunidades Sustentáveis** (principal). Metas 11.2: segurança viária para quem anda a pé e de moto. Meta 11.7: espaços públicos seguros e acessíveis. Secundários: **ODS 9.1** (infraestrutura de qualidade e resiliente) e **ODS 16.6** (instituições eficazes e transparentes, porque o cidadão passa a ver o andamento). Ícone oficial: `amostras/ODS11_icone_oficial_ONU_ptBR.svg`. |
| **Papel da imagem** | A foto vinculada à ocorrência mostra a gravidade (buraco com água, rua escura), ajuda a triagem e a evitar duplicidade, e fica como prova "antes" para conferir o serviço. A amostra de teste é uma **ilustração própria** gerada por script (`amostras/Gerar-Amostras.ps1`). |
| **Indicador proposto** | **PCR30: % de ocorrências concluídas em até 30 dias.** Unidade: % de ocorrências. Fórmula: ocorrências abertas no mês que chegaram a `CONCLUIDA` em até 30 dias de `ABERTAUTC` ÷ ocorrências abertas no mês × 100. A data de conclusão é o `CRIADOUTC` do atendimento que zerou o pendente. O cálculo é feito por região e categoria. Coleta futura: consulta SQL mensal em `OCORRENCIA` + `ATENDIMENTO_OCORRENCIA` num piloto com dados reais. **Não há medição real:** os dados são fictícios. |
| **Ecossistema / parceiros** | Cidadãos e associações de bairro (relatam e acompanham). Subprefeitura (triagem e ordem de serviço). Equipes de conservação viária e de manutenção da iluminação pública (registram atendimentos). Canal oficial de solicitações da Prefeitura (ex.: SP156), como integração **futura, não implementada**. |
| **Independência** | A API roda localmente, sem serviço pago nem IA na aplicação. Em C# não há dependência técnica de VMs/SO de outras disciplinas. |
| **Repositório** | https://github.com/MAJE-Dev/citizentech-zeladoria-api |

## Equipe (dados acadêmicos reais)

| Nome completo | RM | Turma | NomeAluno_RM | Líder |
|---|---|---|---|---|
| Kaio Drago Lima Souza | 556095 | 3ESPH | KaioSouza_RM556095 | **sim** |
| Gustavo Viega Martins | 555885 | 3ESPH | GustavoMartins_RM555885 | não |
| Gustavo Yuji Osugi | 555034 | 3ESPH | GustavoOsugi_RM555034 | não |
| Otávio Santos de Lima Ferrão | 556452 | 3ESPH | OtavioFerrao_RM556452 | não |
| Vitor Rivas Cardoso | 556404 | 3ESPH | VitorCardoso_RM556404 | não |

A mesma identificação está em `src/CitizenTech.Zeladoria.Api/equipe.json`, que é o arquivo lido pela API em `GET /api/equipe` e validado na inicialização. Ela também aparece nos metadados `EQUIPERM`/`EXECUTOR` gravados no banco. Os testes registrados nas evidências foram executados por **VitorCardoso_RM556404**.

## Ambiente real utilizado (E02)

| Item | Versão efetiva | Evidência |
|---|---|---|
| Windows | Windows 11 Home 10.0.26300 (64 bits) | `evidencias/E02_01_Ambiente_Versoes.txt` |
| .NET SDK / TargetFramework | SDK 10.0.404, runtime 10.0.15 / `net10.0` | `E02_01` |
| EF Core / provider | Microsoft.EntityFrameworkCore.SqlServer 10.0.15 | `E02_01` (dotnet list package) |
| Banco | **Microsoft SQL Server 2022 Express LocalDB 16.0.1000.6**, instância `(localdb)\MSSQLLocalDB`, banco `GS_CSHARP_ZELADORIA` | `E02_02_SQL_00_Verificar_Ambiente.txt` |
| Modelagem | diagrams.net (draw.io web), arquivo `modelo/Zeladoria_Modelo.drawio` | `E03_04_Modelo_na_Ferramenta_diagrams.net.png` |
| Cliente HTTP | cliente C# próprio + curl.exe 8.x (Windows) + `testes/api.http` | `E04`–`E08` |
| IDE | Visual Studio 2026 (projeto original do líder) e .NET CLI + terminal PowerShell (execução das evidências) | — |

## Estrutura do ZIP

```
GS_CSHARP_KaioSouza_RM556095/
├── README.md                  (este arquivo: ficha, equipe e como executar)
├── prompts.md                 (uso de IA, finalidade e validação)
├── src/
│   ├── CitizenTech.Zeladoria.slnx
│   ├── CitizenTech.Zeladoria.Api/      (Web API: Program.cs, Modelos.cs, Dados/AppDb.cs, Controllers/, equipe.json)
│   ├── CitizenTech.Zeladoria.Cliente/  (cliente HTTP C#: consome o mesmo contrato)
│   └── CitizenTech.Zeladoria.Tests/    (xUnit: validações)
├── modelo/      (Zeladoria_Modelo.drawio editável, .png exportado, .dbml, DICIONARIO_DADOS.md)
├── sql/sqlserver/  (00_Verificar_Ambiente, 01_Criar_Estrutura, 02_Carga_Ficticia, 03_Verificar_Resultados, 04_Demo_Transacao, Executar-Sql.ps1)
├── testes/      (Executar-Roteiro-T01-T10.ps1, api.http, saida/ com logs reais da API)
├── docs/relatorio.pdf         (D03 preenchido)
├── evidencias/  (indice.md + E01…E09)
└── amostras/    (imagem de teste, ícone ODS 11, Gerar-Amostras.ps1, CREDITOS.md)
```

## Como executar (reprodução)

Todos os comandos são para o **Windows PowerShell**, a partir da pasta raiz extraída (`Set-Location` para ela). Não presumimos SDK nem instância: confira o inventário primeiro.

### 1. Inventário

```powershell
dotnet --list-sdks
Get-Command curl.exe
sqllocaldb info
```

### 2. Banco (SQL Server)

Crie um banco acadêmico **vazio** chamado `GS_CSHARP_ZELADORIA`, pelo SSMS (New Database) ou pelo comando abaixo. A instância `(localdb)\MSSQLLocalDB` é a que usamos. Se a sua for outra, troque o `-Servidor`.

```powershell
.\sql\sqlserver\Executar-Sql.ps1 -Banco master -Consulta "CREATE DATABASE GS_CSHARP_ZELADORIA"
```

Execute os scripts **em ordem: 00 → 01 → 02 → 03**, no SSMS (com o banco selecionado) ou pelo executor incluído:

```powershell
.\sql\sqlserver\Executar-Sql.ps1 -Arquivo .\sql\sqlserver\00_Verificar_Ambiente.sql
.\sql\sqlserver\Executar-Sql.ps1 -Arquivo .\sql\sqlserver\01_Criar_Estrutura.sql
.\sql\sqlserver\Executar-Sql.ps1 -Arquivo .\sql\sqlserver\02_Carga_Ficticia.sql
.\sql\sqlserver\Executar-Sql.ps1 -Arquivo .\sql\sqlserver\03_Verificar_Resultados.sql
```

A linha de base esperada é: 2 regiões, 3 categorias, 3 ocorrências, 0 imagens, 0 atendimentos, e a ocorrência 1 com **20,000 m²** pendentes. O `04_Demo_Transacao.sql` é uma demonstração SQL **separada**: rode-o num banco próprio (por exemplo, `GS_CSHARP_ZELADORIA_DEMO` criado com 01+02), nunca no banco do roteiro da API. Para recomeçar do zero, use um banco novo e vazio. Os scripts não apagam nada automaticamente.

### 3. Conexão segura (fora do ZIP)

A string de conexão **não** está em nenhum arquivo entregue (`appsettings.json` tem `"Banco": ""`). Configure-a com user-secrets. O exemplo usa autenticação do Windows, sem senha:

```powershell
dotnet user-secrets set 'ConnectionStrings:Banco' 'Server=(localdb)\MSSQLLocalDB;Database=GS_CSHARP_ZELADORIA;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True' --project src/CitizenTech.Zeladoria.Api
```

### 4. Restaurar, compilar, testar e executar a API

```powershell
dotnet restore src/CitizenTech.Zeladoria.slnx
dotnet build src/CitizenTech.Zeladoria.slnx
dotnet test src/CitizenTech.Zeladoria.Tests
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/CitizenTech.Zeladoria.Api --no-launch-profile --urls http://localhost:5080
```

Deixe esse terminal aberto. Em `Development` existem o Swagger (`http://localhost:5080/swagger`) e a falha didática controlada (`simularFalha`). Fora desse ambiente, a API rejeita a simulação.

### 5. Cliente HTTP (outro terminal)

```powershell
dotnet run --project src/CitizenTech.Zeladoria.Cliente -- equipe
dotnet run --project src/CitizenTech.Zeladoria.Cliente -- enviar-imagem 1 .\amostras\buraco_poste_apagado_mooca.jpg "Buraco com agua" VitorCardoso_RM556404
dotnet run --project src/CitizenTech.Zeladoria.Cliente -- baixar-imagem 100 .\recuperada.jpg --abrir
dotnet run --project src/CitizenTech.Zeladoria.Cliente -- atender 1 3 EQ_ZEL_MOOCA_01 VitorCardoso_RM556404
dotnet run --project src/CitizenTech.Zeladoria.Cliente -- atender 1 2 EQ_ZEL_MOOCA_01 VitorCardoso_RM556404 --simular-falha
```

Troque o executor pelo seu NomeAluno_RM. O ID da imagem é o que a API devolver.

### 6. Roteiro real T01–T10 (gera as evidências)

Com o banco na linha de base e a API **parada** (o roteiro inicia, reinicia e para a API sozinho):

```powershell
dotnet build src/CitizenTech.Zeladoria.slnx
.\testes\Executar-Roteiro-T01-T10.ps1 -Executor 'VitorCardoso_RM556404'
```

O roteiro grava `evidencias/E04…E08` e um resumo esperado × observado (`E08_00`). Para testes manuais, use `testes/api.http` (VS Code REST Client / Visual Studio) ou o Swagger.

## Contrato da API

| Operação | Método e rota | Entrada | Resposta |
|---|---|---|---|
| Identificação acadêmica | `GET /api/equipe` | — | 200 JSON (CitizenTech, turma, líder, integrantes) |
| Consultas do domínio | `GET /api/regioes`, `GET /api/categorias`, `GET /api/ocorrencias?regiaoId=&categoriaId=&situacao=`, `GET /api/ocorrencias/{id}` | query/rota | 200 JSON; 404 ProblemDetails |
| Registrar ocorrência | `POST /api/ocorrencias` | JSON `{regiaoId, categoriaId, titulo, descricao, endereco, unidade, quantidadeEstimada, executor}` | 201 + Location; 400 |
| Upload de imagem | `POST /api/ocorrencias/{ocorrenciaId}/imagens` | multipart: `Arquivo`, `Titulo`, `Executor` | 201 com ID e metadados; 400 / 404 / 413 / 415 |
| Metadados | `GET /api/imagens/{id}` · `GET /api/ocorrencias/{id}/imagens` | ID | 200 JSON (sem bytes); 404 |
| Conteúdo | `GET /api/imagens/{id}/conteudo` | ID | 200 bytes `image/jpeg`/`image/png`, cabeçalhos `X-Origem-Imagem` e `X-Imagem-SHA256`; 404 |
| **Transação** | `POST /api/ocorrencias/{id}/atendimentos` | JSON `{quantidade, equipeCampoCodigo, executor, simularFalha}` | 201 `COMMIT`; 409 `ROLLBACK` ou pendente insuficiente; 400; 404 |
| Histórico | `GET /api/ocorrencias/{id}/atendimentos` | ID | 200 JSON |

Os erros vêm em **ProblemDetails** (`application/problem+json`). Banco indisponível retorna **503**, sem stack trace e sem string de conexão.

**Transação (R6):** `AtendimentosController.Registrar` abre `BeginTransactionAsync` e faz duas escritas: (1) `UPDATE OCORRENCIA` condicional (`ExecuteUpdateAsync`), que baixa o pendente e ajusta a situação; (2) `INSERT ATENDIMENTO_OCORRENCIA` (`SaveChangesAsync`). Depois vem `CommitAsync`. Com `simularFalha`, a exceção acontece **depois** das duas escritas e o `RollbackAsync` desfaz as duas.

## Resultados T01–T10 (observados em 25/11/2026)

| T | Resultado observado | Situação | Evidência |
|---|---|---|---|
| T01 | GETs 200. POST ocorrência 201. Upload 201 com **ID 100**. SQL: `DATALENGTH` 52484 = arquivo, SHA-256 do SQL = original | aprovado | E04_01, E04_02 |
| T02 | API parada (PID 22292), reiniciada (PID 24984). GET 200 no ID 100. Hash recuperado = original. Imagem exibida pelo navegador a partir da URL da API | aprovado | E05_01, E05_02 |
| T03 | Sem arquivo, 0 bytes e título vazio: 400 com mensagem. Imagens 1 → 1 | aprovado | E08_01 |
| T04 | GIF 415, PNG com texto 415, 2,5 MiB 413. Nenhum registro novo | aprovado | E08_02 |
| T05 | Imagem 999999: 404 (metadados e conteúdo). Upload para ocorrência 999999: 404. Cliente segue utilizável | aprovado | E08_03 |
| T06 | Quantidade 0, -5 e 1.2345: 400. 999: 409. Executor intruso: 400. Ocorrência inexistente: 404. Pendente segue 20,000 e 0 atendimentos | aprovado | E08_04 |
| T07 | 20,000/0 → **COMMIT** → 17,000/1 (EM_ATENDIMENTO) | aprovado | E06_01 |
| T08 | 17,000/1 → falha após UPDATE+INSERT (ID provisório 101) → **ROLLBACK** → 17,000/1 | aprovado | E07_01 |
| T09 | Modelo aberto no diagrams.net. Relatório e mapa de requisitos/código | aprovado | E03_04, E09, docs/relatorio.pdf |
| T10 | API parada: cliente mostra "[ERRO DE COMUNICAÇÃO]" (exit 2) e o banco não muda. Depois do reinício, 200 | aprovado | E08_05 |

Resumo automático: **54 verificações, 0 divergências** (`E08_00`). A primeira execução encontrou 2 divergências, que foram corrigidas e documentadas em `E09_02`.

## Documentos

- Relatório D03 preenchido: [`docs/relatorio.pdf`](docs/relatorio.pdf)
- Índice de evidências (E01–E09, T e R): [`evidencias/indice.md`](evidencias/indice.md)
- Modelo e dicionário: [`modelo/`](modelo/)
- Uso de IA: [`prompts.md`](prompts.md)
- Fontes e créditos de imagens: [`amostras/CREDITOS.md`](amostras/CREDITOS.md)

**Credenciais:** nenhum arquivo do ZIP contém senha ou string de conexão. As pastas `bin/`, `obj/` e `.vs/` não são entregues.
