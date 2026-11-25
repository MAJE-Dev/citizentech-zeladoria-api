-- GS 2026.2 C# Software Development - Zeladoria API Urbana - 3ESPH
-- Banco escolhido: Microsoft SQL Server (execução da equipe: SQL Server 2022 Express LocalDB).
-- Executar no banco acadêmico VAZIO GS_CSHARP_ZELADORIA, ANTES do 01.
-- Não cria login, usuário ou banco e não apaga nada.

SELECT @@VERSION AS VERSAO_COMPLETA;

SELECT @@SERVERNAME                       AS SERVIDOR_INSTANCIA,
       DB_NAME()                          AS BANCO_ATUAL,
       SERVERPROPERTY('Edition')          AS EDICAO,
       SERVERPROPERTY('ProductVersion')   AS VERSAO_PRODUTO,
       SERVERPROPERTY('ProductLevel')     AS NIVEL,
       SUSER_SNAME()                      AS LOGIN_CONECTADO;

SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') AS PODE_CRIAR_TABELA,
       (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS TABELAS_DE_USUARIO_EXISTENTES;
-- Esperado antes do 01: PODE_CRIAR_TABELA = 1 e TABELAS_DE_USUARIO_EXISTENTES = 0.
