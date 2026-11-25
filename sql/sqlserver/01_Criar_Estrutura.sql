-- GS 2026.2 C# - Zeladoria API Urbana - SQL Server
-- Executar no banco acadêmico vazio GS_CSHARP_ZELADORIA. Não executa CREATE DATABASE nem DROP.
-- Ordem: 00 -> 01 -> 02 -> 03. O 04 é demonstração SQL separada (usar banco próprio).
-- A API NÃO chama EnsureCreated/Migrate: a estrutura vem deste script, com tipos e restrições explícitos.

-- 1) Território: subprefeitura/bairro onde a ocorrência está. 1 região -> 0..N ocorrências.
CREATE TABLE REGIAO (
    ID      int IDENTITY(100,1) NOT NULL CONSTRAINT PK_REGIAO PRIMARY KEY,
    NOME    nvarchar(100) NOT NULL,
    BAIRRO  nvarchar(80)  NOT NULL,
    CIDADE  nvarchar(80)  NOT NULL
);

-- 2) Tipo de problema urbano. 1 categoria -> 0..N ocorrências.
CREATE TABLE CATEGORIA_OCORRENCIA (
    ID    int IDENTITY(100,1) NOT NULL CONSTRAINT PK_CATEGORIA_OCORRENCIA PRIMARY KEY,
    NOME  nvarchar(60) NOT NULL CONSTRAINT UQ_CATEGORIA_OCORRENCIA_NOME UNIQUE
);

-- 3) Ocorrência registrada pelo cidadão, com "saldo" de serviço pendente
--    (m2 de pavimento, pontos de luz, m3 de entulho...). Cada ocorrência tem 1 região e 1 categoria.
CREATE TABLE OCORRENCIA (
    ID                  int IDENTITY(100,1) NOT NULL CONSTRAINT PK_OCORRENCIA PRIMARY KEY,
    REGIAOID            int            NOT NULL,
    CATEGORIAID         int            NOT NULL,
    TITULO              nvarchar(100)  NOT NULL,
    DESCRICAO           nvarchar(500)  NOT NULL,
    ENDERECO            nvarchar(150)  NOT NULL,
    UNIDADE             nvarchar(10)   NOT NULL,
    QUANTIDADEESTIMADA  decimal(12,3)  NOT NULL,
    QUANTIDADEPENDENTE  decimal(12,3)  NOT NULL,
    SITUACAO            nvarchar(20)   NOT NULL,
    ABERTAUTC           datetime2(6)   NOT NULL,
    CONSTRAINT FK_OCORRENCIA_REGIAO    FOREIGN KEY (REGIAOID)    REFERENCES REGIAO(ID),
    CONSTRAINT FK_OCORRENCIA_CATEGORIA FOREIGN KEY (CATEGORIAID) REFERENCES CATEGORIA_OCORRENCIA(ID),
    CONSTRAINT CK_OCORRENCIA_ESTIMADA  CHECK (QUANTIDADEESTIMADA > 0),
    CONSTRAINT CK_OCORRENCIA_PENDENTE  CHECK (QUANTIDADEPENDENTE >= 0 AND QUANTIDADEPENDENTE <= QUANTIDADEESTIMADA),
    CONSTRAINT CK_OCORRENCIA_SITUACAO  CHECK (SITUACAO IN (N'ABERTA', N'EM_ATENDIMENTO', N'CONCLUIDA')),
    -- Regra de consistência: só é CONCLUIDA quando não resta nada pendente.
    CONSTRAINT CK_OCORRENCIA_CONCLUSAO CHECK ((SITUACAO = N'CONCLUIDA' AND QUANTIDADEPENDENTE = 0)
                                           OR (SITUACAO <> N'CONCLUIDA' AND QUANTIDADEPENDENTE > 0))
);

-- 4) Imagem (foto do problema) em BYTES no banco. Cada imagem tem 1 ocorrência; ocorrência admite 0..N imagens.
CREATE TABLE IMAGEM_OCORRENCIA (
    ID            int IDENTITY(100,1) NOT NULL CONSTRAINT PK_IMAGEM_OCORRENCIA PRIMARY KEY,
    OCORRENCIAID  int             NOT NULL,
    TITULO        nvarchar(100)   NOT NULL,
    NOMEARQUIVO   nvarchar(200)   NOT NULL,
    MIME          nvarchar(30)    NOT NULL,
    TAMANHOBYTES  int             NOT NULL,
    HASHSHA256    char(64)        NOT NULL,
    DADOS         varbinary(max)  NOT NULL,
    EQUIPERM      nvarchar(20)    NOT NULL,
    EXECUTOR      nvarchar(80)    NOT NULL,
    ENVIADAUTC    datetime2(6)    NOT NULL,
    CONSTRAINT FK_IMAGEM_OCORRENCIA FOREIGN KEY (OCORRENCIAID) REFERENCES OCORRENCIA(ID),
    CONSTRAINT CK_IMAGEM_MIME       CHECK (MIME IN (N'image/png', N'image/jpeg')),
    CONSTRAINT CK_IMAGEM_TAMANHO    CHECK (TAMANHOBYTES > 0 AND TAMANHOBYTES <= 2097152
                                           AND TAMANHOBYTES = DATALENGTH(DADOS))
);

-- 5) Atendimento (registro de andamento executado em campo). Cada atendimento tem 1 ocorrência;
--    ocorrência admite 0..N atendimentos. A API grava este INSERT junto com o UPDATE do pendente.
CREATE TABLE ATENDIMENTO_OCORRENCIA (
    ID                 int IDENTITY(100,1) NOT NULL CONSTRAINT PK_ATENDIMENTO_OCORRENCIA PRIMARY KEY,
    OCORRENCIAID       int            NOT NULL,
    QUANTIDADE         decimal(12,3)  NOT NULL,
    EQUIPECAMPOCODIGO  nvarchar(40)   NOT NULL,
    EQUIPERM           nvarchar(20)   NOT NULL,
    EXECUTOR           nvarchar(80)   NOT NULL,
    CRIADOUTC          datetime2(6)   NOT NULL,
    CONSTRAINT FK_ATENDIMENTO_OCORRENCIA FOREIGN KEY (OCORRENCIAID) REFERENCES OCORRENCIA(ID),
    CONSTRAINT CK_ATENDIMENTO_QTD        CHECK (QUANTIDADE > 0)
);

-- Índices das FKs (consultas por vínculo). FKs sem ON DELETE = exclusão restrita (NO ACTION).
CREATE INDEX IX_OCORRENCIA_REGIAOID              ON OCORRENCIA(REGIAOID);
CREATE INDEX IX_OCORRENCIA_CATEGORIAID           ON OCORRENCIA(CATEGORIAID);
CREATE INDEX IX_IMAGEM_OCORRENCIA_OCORRENCIAID   ON IMAGEM_OCORRENCIA(OCORRENCIAID);
CREATE INDEX IX_ATENDIMENTO_OCORRENCIA_OCORRENCIAID ON ATENDIMENTO_OCORRENCIA(OCORRENCIAID);

SELECT name AS TABELA_CRIADA, create_date FROM sys.tables WHERE is_ms_shipped = 0 ORDER BY create_date;
