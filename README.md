# 🏙️ CitizenTech Zeladoria — API de Gestão de Ocorrências Urbanas (.NET 10)
> A CitizenTech.Zeladoria é uma API RESTful em .NET 10 (C#) que conecta cidadãos e a administração pública para otimizar a zeladoria urbana. A solução permite registrar, categorizar e acompanhar em tempo real demandas de infraestrutura (como buracos em vias e falhas de iluminação), substituindo processos manuais por um fluxo transparente, rastreável e eficiente.

## 📋 Ficha da CitizenTech

| Campo | Detalhe |
| :--- | :--- |
| **Nome da CitizenTech** | Zeladoria API Urbana (`citizentech-zeladoria-api`) |
| **Cidade e bairro** | São Paulo - SP (escopo municipal) |
| **Cidadão beneficiado** | Moradores da cidade, pedestres e motoristas que convivem com falhas de infraestrutura urbana |
| **Problema, em uma frase** | Dificuldade e morosidade dos cidadãos em reportar e acompanhar a resolução de problemas de infraestrutura pública. |
| **Como funciona** | Uma REST API assíncrona em .NET 10 onde cidadãos registram ocorrências urbanas com envio de imagem (`multipart/form-data`), armazenando bytes reais e metadados no banco em memória, enquanto a gestão filtra, atualiza status, acompanha comentários e executa operações transacionais com segurança. |
| **O que cada parte faz** | **Controllers (`OcorrenciasController`):** Expõe os endpoints REST (`GET`, `GET/{id}`, `GET/{id}/imagem`, `POST`, `POST/transacional`, `PATCH/{id}/status`, `DELETE`) com `[FromForm]`/`IFormFile` e validações de requisição.<br>**Services (`OcorrenciaService` / `IOcorrenciaService`):** Executa as regras de negócio, conversão de `IFormFile` para `byte[]` e metadados, validações de arquivo/regras, mapeamento de DTOs (`OcorrenciaDtos`), consultas assíncronas com EF Core/LINQ e controle de transações (`Commit` / `Rollback`).<br>**Data (`AppDbContext`):** Gerencia as 5 entidades relacionadas no banco de dados em memória via Entity Framework Core e transações atômicas.<br>**Middlewares (`ErrorHandlingMiddleware`):** Trata falhas e exceções de forma centralizada e retorna respostas HTTP/JSON consistentes sem efeitos colaterais.<br>**Tests (`CitizenTech.Zeladoria.Tests`):** Valida a lógica de negócio, validações, fluxos transacionais e cenários de falha de forma isolada com testes unitários em xUnit. |
| **Link do repositório** | https://github.com/MAJE-Dev/citizentech-zeladoria-api |
| **Indicador de impacto** | Redução no tempo de resposta para reparações de infraestrutura e aumento do percentual de ocorrências urbanas concluídas dentro do prazo. |
| **ODS conectados** | **ODS 11** (Cidades e Comunidades Sustentáveis) e **ODS 9** (Indústria, Inovação e Infraestrutura) |

## 🏗️ Arquitetura e Stack Tecnológica
> Principais recursos e ferramentas que sustentam a arquitetura e o funcionamento da aplicação.
> 
A aplicação segue uma separação clara de responsabilidades entre **Controllers**, **Services**, **DTOs**, **Entidades de Domínio** e **Camada de Dados (`DbContext`)**, utilizando programação assíncrona (`async`/`await`) e **Entity Framework Core**.

| Camada / Componente | Tecnologia / Abordagem | Responsabilidade |
| :--- | :--- | :--- |
| **Runtime & Framework** | **.NET 10 / ASP.NET Core Web API** | Exposição dos endpoints REST, roteamento, *model binding* (`[FromForm]`, `[FromBody]`) e respostas HTTP |
| **Acesso a Dados** | **Entity Framework Core (In-Memory)** | Mapeamento de entidades, consultas LINQ assíncronas e controle de transações |
| **Upload & Mídia** | **`IFormFile` + `MemoryStream` (`byte[]`)** | Recebimento via `multipart/form-data`, leitura em memória, persistência de bytes e metadados técnicos |
| **Contratos (DTOs)** | **`OcorrenciaDtos.cs`** | Desacoplamento entre entrada/saída da API e entidades internas do domínio |
| **Documentação** | **Swagger / OpenAPI** | Exploração interativa dos contratos e testes manuais dos endpoints |
| **Testes** | **Projeto de Testes Automatizados** | Validação de comportamentos, regras de negócio, fluxos transacionais e casos de erro |

## 🗄️ Modelo de Dados e Relacionamentos

O domínio é estruturado em **cinco entidades principais inter-relacionadas**, com chaves primárias (`PK`), chaves estrangeiras (`FK`) e cardinalidades definidas:

* **`Ocorrencias` (`1` — entidade central):** Armazena `Id`, `Titulo`, `Descricao`, `Endereco`, `DataRegistro`, chaves estrangeiras (`CategoriaId`, `StatusId`, `UsuarioId`), campos de imagem (`ImagemDados`, `ImagemNome`, `ImagemTipo`, `ImagemTamanho`) e navegação para comentários.
* **`Categorias` (`1:N` com `Ocorrencias`):** Classifica o tipo de demanda urbana (ex.: *Iluminação Pública*, *Pavimentação*, *Limpeza Urbana*).
* **`StatusOcorrencias` (`1:N` com `Ocorrencias`):** Define o estágio da ocorrência no fluxo operacional (ex.: *Aberta*, *Em andamento*, *Concluída*).
* **`Usuarios` (`1:N` com `Ocorrencias` e `1:N` com `Comentarios`):** Representa o solicitante ou responsável vinculado aos registros.
* **`Comentarios` (`N:1` com `Ocorrencias` e `N:1` com `Usuarios`):** Registra atualizações, observações técnicas e histórico de atendimento de cada ocorrência.

> **Observação sobre Persistência em Memória:** Este projeto utiliza banco de dados em memória do EF Core para agilidade em desenvolvimento e execução de testes. Por residir na memória volátil do processo, os dados e registros criados durante uma execução são descartados ao encerrar ou reiniciar a aplicação.

### Printscreen das tabelas de domínio do DB
<img width="769" height="331" alt="tabelas-de-dominio-database" src="https://github.com/user-attachments/assets/001fb7b4-d035-4f2b-bb61-ec4926333327" />

---

## 🖼️ Gestão de Imagens e Metadados (C4)

Em conformidade com a exigência de armazenamento de bytes reais no banco e metadados técnicos:

1. **Recebimento via `multipart/form-data`:** Os endpoints de criação recebem o arquivo por meio de `IFormFile` (`[FromForm]`).
2. **Conversão para Binário (`byte[]`):** O conteúdo do arquivo é lido de forma assíncrona via `MemoryStream` e convertido em `byte[]` (`ImagemDados`).
3. **Metadados Técnicos:** São persistidos junto à ocorrência:
   * `ImagemNome` (nome original do arquivo)
   * `ImagemTipo` (tipo MIME, ex.: `image/png`, `image/jpeg`)
   * `ImagemTamanho` (tamanho em bytes)
4. **Consulta por ID e Tamanho:** O endpoint `GET /api/Ocorrencias/{id}/imagem` recupera os bytes armazenados, devolve o arquivo com seu `ContentType` e informa o tamanho em bytes (ex.: via cabeçalho HTTP `X-Imagem-Tamanho-Bytes`).

---

## 🔄 Transações e Consistência de Dados (C5)

O endpoint **`POST /api/Ocorrencias/transacional`** executa uma operação composta (por exemplo, criação da ocorrência e registro inicial de comentário) dentro de uma transação explícita do EF Core:

* **Sucesso (`simularErro=false`):** Ambas as gravações são confirmadas de forma atômica (**`Commit`**).
* **Falha simulada (`simularErro=true`):** Uma exceção é disparada após a preparação/escrita inicial, acionando o **`Rollback`** e garantindo que nenhuma das alterações seja mantida no banco.

---

## 🛡️ Validações e Tratamento de Falhas (C6)

A API adota respostas HTTP consistentes e prevenção de efeitos colaterais em cenários inválidos:

* **Validação de campos e regras:** Retorno de `400 Bad Request` quando dados obrigatórios ou regras de domínio não são atendidos.
* **Validação de arquivo de imagem:** Verificação de arquivo vazio, formato/MIME type não permitido ou tamanho acima do limite aceitável.
* **Recursos inexistentes:** Retorno de `404 Not Found` com mensagem clara quando um `id` de ocorrência ou status não existe.
* **Consistência em falhas:** Exceções e erros transacionais não deixam dados parciais ou inconsistentes na base.

---

## ⚙️ Configurações e Instalação
> Guia rápido de configuração do ambiente, compilação e inicialização da API localmente.

### 1. Clonar o repositório
Clone o repositório para o seu diretório de desenvolvimento local:
```bash
git clone <URL_DO_SEU_REPOSITORIO>
cd <NOME_DA_PASTA_DO_PROJETO>
```

### 2. Restaurar e compilar as dependências
Restaure os pacotes `NuGet` e execute a compilação da solução:
```bash
dotnet restore
dotnet build
```

### 3. Execute a aplicação:
Inicie o servidor de desenvolvimento da API:
```bash
dotnet run
```

Após iniciar, abra o Swagger no endereço exibido no console (ex: `http://localhost:5xxx/swagger`).


## 📌 4. Endpoints, Payloads
> Referência das rotas REST disponíveis para cadastro, consulta, atualização e remoção de ocorrências urbanas + **Evidências de execução da aplicação**. ***Observação:*** Os endpoints `POST /api/Ocorrencias` e `POST /api/Ocorrencias/transacional` utilizam `[FromForm]` (`multipart/form-data`) para permitir o envio de campos e arquivo de imagem (`IFormFile`) na mesma requisição.

---

### 1. Criar Ocorrência com Imagem (`C3` · `C4` · `C6`)
* **Rota:** `POST /api/Ocorrencias`
* **Formato de envio:** `multipart/form-data`
* **Campos enviados:**
  * `Titulo` (`string`): `"Buraco na via pública"`
  * `Descricao` (`string`): `"Buraco grande atrapalhando o trânsito na avenida principal."`
  * `Endereco` (`string`): `"Av. Paulista, 1000 - São Paulo, SP"`
  * `CategoriaId` (`int`): `1`
  * `UsuarioId` (`int`): `1`
  * `Imagem` (`IFormFile`): arquivo `.png` ou `.jpg`
* **Retorno esperado (`201 Created`):**
  ```json
  {
    "id": 1,
    "titulo": "Buraco na via pública",
    "descricao": "Buraco grande atrapalhando o trânsito na avenida principal.",
    "categoriaNome": "Iluminação Pública",
    "endereco": "Av. Paulista, 1000 - São Paulo, SP",
    "statusDescricao": "Aberta",
    "dataRegistro": "2026-11-18T21:23:58.5496572Z",
    "usuarioNome": "Carlos Silva",
    "imagemUrl": "/api/Ocorrencias/1/imagem"
  }


