# 🏙️ CitizenTech Zeladoria — API de Gestão de Ocorrências Urbanas (.NET 10)
> A CitizenTech.Zeladoria é uma API RESTful em .NET 10 (C#) que conecta cidadãos e a administração pública para otimizar a zeladoria urbana. A solução permite registrar, categorizar e acompanhar em tempo real demandas de infraestrutura (como buracos em vias e falhas de iluminação), substituindo processos manuais por um fluxo transparente, rastreável e eficiente.


## 👥Desenvolvedores
| Turma | Nome | RM |
| :--- | :--- | :--- |
| 3ESPH | Gustavo Viega Martins | RM555885 |
| 3ESPH | Gustavo Yuji Osugi | RM555034 |
| 3ESPH | Kaio Drago Lima Souza | RM556095 |
| 3ESPH | Otávio Santos de Lima Ferrão | RM556452 |
| 3ESPH | Vitor Rivas Cardoso | RM556404 |


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


## 🏗️ 1. Arquitetura e Stack Tecnológica
> Principais recursos e ferramentas que sustentam a arquitetura e o funcionamento da aplicação.

A aplicação segue uma separação clara de responsabilidades entre **Controllers**, **Services**, **DTOs**, **Entidades de Domínio** e **Camada de Dados (`DbContext`)**, utilizando programação assíncrona (`async`/`await`) e **Entity Framework Core**.

| Camada / Componente | Tecnologia / Abordagem | Responsabilidade |
| :--- | :--- | :--- |
| **Runtime & Framework** | **.NET 10 / ASP.NET Core Web API** | Exposição dos endpoints REST, roteamento, *model binding* (`[FromForm]`, `[FromBody]`) e respostas HTTP |
| **Acesso a Dados** | **Entity Framework Core (In-Memory)** | Mapeamento de entidades, consultas LINQ assíncronas e controle de transações |
| **Upload & Mídia** | **`IFormFile` + `MemoryStream` (`byte[]`)** | Recebimento via `multipart/form-data`, leitura em memória, persistência de bytes e metadados técnicos |
| **Contratos (DTOs)** | **`OcorrenciaDtos.cs`** | Desacoplamento entre entrada/saída da API e entidades internas do domínio |
| **Documentação** | **Swagger / OpenAPI** | Exploração interativa dos contratos e testes manuais dos endpoints |
| **Testes** | **Projeto de Testes Automatizados** | Validação de comportamentos, regras de negócio, fluxos transacionais e casos de erro |

## 🗄️ 2. Banco de Dados *(In-Memory)*

O domínio é estruturado em **cinco entidades principais inter-relacionadas**, com chaves primárias (`PK`), chaves estrangeiras (`FK`) e cardinalidades definidas:

- **Ocorrencias (Entidade Central):** Registro principal da demanda urbana (`Id`, `Titulo`, `Descricao`, `Endereco`, `DataRegistro`), vínculos (`CategoriaId`, `StatusId`, `UsuarioId`) e mídia binária com metadados (`ImagemDados`, `ImagemNome`, `ImagemTipo`, `ImagemTamanho`).
- **Categorias:** Tipo da demanda urbana (ex.: *Iluminação Pública*, *Pavimentação*, *Limpeza Urbana*).
- **StatusOcorrencias:** Etapa do fluxo operacional (ex.: *Aberta*, *Em andamento*, *Concluída*).
- **Usuarios:** Solicitantes e operadores vinculados às ocorrências e aos comentários.
- **Comentarios:** Histórico de atualizações e observações técnicas de cada ocorrência.

> **Observação sobre Persistência em Memória:** Este projeto utiliza banco de dados em memória do EF Core para agilidade em desenvolvimento e execução de testes. Por residir na memória volátil do processo, os dados e registros criados durante uma execução são descartados ao encerrar ou reiniciar a aplicação.

<div align="center">
  <img width="769" height="331" alt="tabelas-de-dominio-database" src="https://github.com/user-attachments/assets/001fb7b4-d035-4f2b-bb61-ec4926333327" />
  <p><em>Figura 1 — Tabelas de domínio populadas via Seed no banco de dados em memória (EF Core).</em></p>
</div>



## ⚙️ 3. Ambiente e Execução *(Setup)*
> Guia rápido de configuração do ambiente, compilação e inicialização da API localmente.

### 3.1 Clonar o repositório
Clone o repositório para o seu diretório de desenvolvimento local:
```bash
git clone <URL_DO_SEU_REPOSITORIO>
cd <NOME_DA_PASTA_DO_PROJETO>
```

### 3.2 Restaurar e compilar as dependências
Restaure os pacotes `NuGet` e execute a compilação da solução:
```bash
dotnet restore
dotnet build
```

### 3.3 Execute a aplicação:
Inicie o servidor de desenvolvimento da API:
```bash
dotnet run
```

Após iniciar, abra o **Swagger** no endereço exibido no cmd (ex: `http://localhost:5xxx/swagger`).


## 📌 4. Endpoints da API
> Referência das rotas REST disponíveis para cadastro, consulta, atualização e remoção de ocorrências urbanas + **Evidências de execução da aplicação**. ***Observação:*** Os endpoints `POST /api/Ocorrencias` e `POST /api/Ocorrencias/transacional` utilizam `[FromForm]` (`multipart/form-data`) para permitir o envio de campos e arquivo de imagem (`IFormFile`) na mesma requisição.

- ***Observação:***  O endpoint **`POST /api/Ocorrencias/transacional`** executa uma operação composta (por exemplo, criação da ocorrência e registro inicial de comentário) dentro de uma transação explícita do EF Core:

* **Sucesso (`simularErro=false`):** Ambas as gravações são confirmadas de forma atômica (**`Commit`**).
* **Falha simulada (`simularErro=true`):** Uma exceção é disparada após a preparação/escrita inicial, acionando o **`Rollback`** e garantindo que nenhuma das alterações seja mantida no banco.



### 🟢 4.1 Cadastrar Ocorrência (`POST/api/Ocorrencias`)
Registra uma nova ocorrência urbana no sistema.
* **Retorno de sucesso:** `201 Created`

<div align="center">
  <img width="1276" height="764" alt="post-method-log" src="https://github.com/user-attachments/assets/87966bf0-07d3-40f5-a5bd-da68afb3a305" />
  <p><em>Figura 2 — Execução do endpoint POST e logs da aplicação durante o registro da ocorrência.</em></p>
</div>


### 🔵 4.2 Listar Ocorrências (`GET/api/Ocorrencias`)
Retorna a listagem de todas as ocorrências registradas (suporta filtros por `categoria` e `status`).
* **Retorno de sucesso:** `200 OK`

<div align="center">
  <img width="1134" height="824" alt="get-method-log" src="https://github.com/user-attachments/assets/b1ac0b0b-b979-4fe2-b8d9-405c4da135ec" />
  <p><em>Figura 3 — Execução do endpoint GET e logs da aplicação na consulta de ocorrências.</em></p>
</div>


### 🔵 4.3 Listar Ocorrência por ID (`GET/api/Ocorrencias/{id}`)
Consulta os detalhes de uma ocorrência específica pelo seu identificador único.
* **Retorno de sucesso:** `200 OK`

<div align="center">
  <img width="1135" height="660" alt="get-id-method-log" src="https://github.com/user-attachments/assets/0570cd82-ecee-49de-b34b-196b34a14a32" />
  <p><em>Figura 4 — Consulta de ocorrência por ID via GET e logs correspondentes da aplicação.</em></p>
</div>


### 🟢 4.4 Criação Transacional — Commit (`POST/api/Ocorrencias/transacional?simularErro=false`)
#### Cenário 1 — `simularErro=false` (201 Created / Commit)
- Confirma e persiste todas as operações da transação de forma atômica, retornando os dados da ocorrência criada.

<div align="center">
  <img width="1136" height="658" alt="post-method-commit-log" src="https://github.com/user-attachments/assets/52dcd01e-b956-4759-8689-a26577227b76" />
  <p><em>Figura 5 — Execução de operação transacional com confirmação (Commit) e logs da aplicação.</em></p>
</div>


### 🟢 4.5 Criação Transacional — Rollback (`POST/api/Ocorrencias/transacional?simularErro=false`)
#### Cenário 2 — `simularErro=true` (400 Bad Request / Rollback):
- Simula uma falha após o início da operação e reverte toda a transação, sem deixar registros parciais:

<div align="center">
  <img width="1136" height="602" alt="post-method-rollback-log" src="https://github.com/user-attachments/assets/a2c43c63-dbbf-4a0f-8f25-791e1ddee8e9" />
  <p><em>Figura 6 — Execução de operação transacional com falha simulada, reversão (Rollback) e logs da aplicação.</em></p>
</div>


### 🟠 4.6 Atualizar Status da Ocorrência (`PATCH/api/Ocorrencias/{id}/status`)
Atualiza os dados ou o status de uma ocorrência existente.
- Retorno esperado **(200 OK):**

***Obs:*** *Retorna a ocorrência com o status atualizado (ou `404 Not Found` caso o ID da ocorrência ou status seja inválido/inexistente).*

<div align="center">
  <img width="1134" height="658" alt="patch-method-id-log" src="https://github.com/user-attachments/assets/12d4b897-1b30-4a3c-aadb-558d2a930adb" />
  <p><em>Figura 7 — Atualização de status da ocorrência por ID via PATCH e logs da aplicação.</em></p>
</div>


### 🔴 4.7 Remover Ocorrência (`DELETE/api/ocorrencias/{id}`)
Remove uma ocorrência do sistema.
* **Retorno de sucesso:** `204 No Content`

<div align="center">
  <img width="1138" height="672" alt="delete-method-id-log" src="https://github.com/user-attachments/assets/069f469c-ece9-4d15-a9f2-7f1f3c055b23" />
  <p><em>Figura 8 — Remoção de ocorrência por ID via DELETE e logs da aplicação.</em></p>
</div>


## 🛡️ 5. Validações e Tratamento de Falhas
> Tratamento de requisições malformadas, exceções da aplicação e consistência de dados.


A API centraliza o tratamento de exceções e aplica validações preventivas para garantir respostas HTTP previsíveis, segurança no upload de arquivos e integridade dos dados:

- **Dados e regras de domínio (`400 Bad Request`):** Bloqueia requisições com campos obrigatórios ausentes, formatos inválidos ou violação de regras de negócio antes da persistência.
- **Upload e processamento de imagem (`400 Bad Request`):** Valida arquivo vazio, tipo MIME não permitido e limite de tamanho antes da conversão e manipulação binária (`byte[]`).
- **Recursos não encontrados (`404 Not Found`):** Retorna erro padronizado quando o `id` informado (ocorrência, categoria ou status) não existe na base.
- **Resiliência e consistência transacional:** Exceções não tratadas são interceptadas de forma centralizada — evitando exposição de detalhes internos — e falhas em operações transacionais acionam `Rollback`, impedindo gravações parciais ou estados inconsistentes.


## 🧪 6. Testes Automatizados *(Build)*
> Validação automatizada das regras de negócio, garantia de qualidade e integridade da camada de serviços utilizando xUnit.

Os testes funcionais e de integração foram executados diretamente através da interface do **Swagger UI** utilizando a base de dados *In-Memory* populada via *Seed*, validando tanto os fluxos principais de CRUD quanto o armazenamento/consulta de imagens, validações de entrada e operações atômicas.

| ID | Cenário / Descrição do Teste | Endpoint | Resultado Esperado | Status |
| :--- | :--- | :--- | :--- | :--- |
| **T01** | Listar ocorrências (geral e com filtros por Categoria/Status) | `GET/api/Ocorrencias?categoriaId=1` | `200 OK` (Lista de ocorrências filtrada) | ✅ Aprovado |
| **T02** | Obter ocorrência existente por ID | `GET/api/Ocorrencias/{id}` | `200 OK` (Dados e metadados da ocorrência) | ✅ Aprovado |
| **T03** | Tentar obter ocorrência com ID inexistente | `GET/api/Ocorrencias/999` | `404 Not Found` | ✅ Aprovado |
| **T04** | Criar nova ocorrência com upload de imagem (`multipart/form-data`) | `POST/api/Ocorrencias` | `201 Created` (Bytes e metadados salvos) | ✅ Aprovado |
| **T05** | Consultar imagem da ocorrência por ID e tamanho | `GET/api/Ocorrencias/{id}/imagem` | `200 OK` (Retorno binário + tamanho em bytes) | ✅ Aprovado |
| **T06** | Tentar criar ocorrência com dados ou arquivo inválidos | `POST/api/Ocorrencias` | `400 Bad Request` (Erros de validação) | ✅ Aprovado |
| **T07** | Atualizar status de uma ocorrência existente | `PATCH/api/Ocorrencias/{id}/status` | `200 OK` (Ocorrência atualizada) | ✅ Aprovado |
| **T08** | Remover uma ocorrência com sucesso | `DELETE/api/Ocorrencias/{id}` | `204 No Content` | ✅ Aprovado |
| **T09** | Executar operação transacional com sucesso (`Commit`) | `POST/api/Ocorrencias/transacional?simularErro=false` | `201 Created` (Ocorrência e comentário gravados) | ✅ Aprovado |
| **T10** | Executar operação transacional com falha simulada (`Rollback`) | `POST/api/Ocorrencias/transacional?simularErro=true` | `400 Bad Request` (Operação revertida sem persistência) | ✅ Aprovado |

### 6.1 Explicação Técnica: 
- **Persistência Binária e Consulta de Imagem (T04 e T05):** No envio via `multipart/form-data`, o arquivo `IFormFile` é convertido para `byte[]` e armazenado junto aos seus metadados técnicos (nome, tipo MIME e tamanho). A consulta por ID devolve o conteúdo binário e o tamanho correspondente.
- **Consistência Transacional (T09 e T10):** Ao executar o endpoint transacional com `simularErro=false`, a ocorrência e o comentário inicial são persistidos em conjunto (`Commit`). Com `simularErro=true`, a exceção simulada interrompe o fluxo e aciona a reversão (`Rollback`), garantindo que nenhum registro parcial seja gravado na base em memória.
- **Blindagem de Entrada (T06):** O envio de campos obrigatórios vazios, IDs inválidos ou arquivos fora do padrão é interceptado pelas *Data Annotations* nos DTOs e regras de validação, devolvendo `400 Bad Request` com mensagens claras antes de alterar o estado da base.


<div align="center">
  <img width="1045" height="914" alt="testes-automatizados" src="https://github.com/user-attachments/assets/85402327-699f-4940-aa85-3145c3fd4835" />
  <p><em>Figura 9 — Execução e resultado da suíte de testes automatizados da aplicação.</em></p>
</div>


<div align="center">
  <p>© 2026 MajeDev — CitizenTech Zeladoria (Global Solution 2026). Todos os direitos reservados.</p>
</div>
