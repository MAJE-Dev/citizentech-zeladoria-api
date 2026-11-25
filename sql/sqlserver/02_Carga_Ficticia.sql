-- GS 2026.2 C# - Zeladoria API Urbana - SQL Server
-- Carga de DADOS FICTÍCIOS do domínio (endereços e ocorrências inventados para teste).
-- Não contém imagem nem bytes de preenchimento: a imagem entra por upload real na API.
-- Não repetir sobre registros existentes: para recomeçar, usar um banco acadêmico novo e vazio.
-- IDs fixos 1..3 com IDENTITY_INSERT; novos registros recebem IDs a partir de 100 (podem ter lacunas após rollback).
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM REGIAO) OR EXISTS (SELECT 1 FROM CATEGORIA_OCORRENCIA) OR EXISTS (SELECT 1 FROM OCORRENCIA)
    THROW 50001, 'Carga ja aplicada neste banco. Use um banco academico novo e vazio para recomecar.', 1;

BEGIN TRANSACTION;

SET IDENTITY_INSERT REGIAO ON;
INSERT INTO REGIAO (ID, NOME, BAIRRO, CIDADE) VALUES
    (1, N'Subprefeitura Mooca',        N'Mooca',        N'São Paulo'),
    (2, N'Subprefeitura Vila Mariana', N'Vila Mariana', N'São Paulo');
SET IDENTITY_INSERT REGIAO OFF;

SET IDENTITY_INSERT CATEGORIA_OCORRENCIA ON;
INSERT INTO CATEGORIA_OCORRENCIA (ID, NOME) VALUES
    (1, N'Pavimentação - buraco na via'),
    (2, N'Iluminação pública'),
    (3, N'Limpeza - descarte irregular');
SET IDENTITY_INSERT CATEGORIA_OCORRENCIA OFF;

SET IDENTITY_INSERT OCORRENCIA ON;
INSERT INTO OCORRENCIA (ID, REGIAOID, CATEGORIAID, TITULO, DESCRICAO, ENDERECO, UNIDADE,
                        QUANTIDADEESTIMADA, QUANTIDADEPENDENTE, SITUACAO, ABERTAUTC) VALUES
    (1, 1, 1, N'Buracos com acúmulo de água na esquina',
        N'Cenário fictício inspirado na escuta de 14/11/2026: buracos fundos que enchem de água na chuva; pedestres e motociclistas caem.',
        N'Rua Fictícia das Oficinas x Rua Exemplo (Mooca)', N'm2', 20.000, 20.000, N'ABERTA', '2026-11-15T12:00:00'),
    (2, 1, 2, N'Postes apagados no trajeto do ponto de ônibus',
        N'Cenário fictício: metade dos pontos de luz apagados após as 19h no quarteirão.',
        N'Rua Fictícia do Ponto, quadra 2 (Mooca)', N'un', 6.000, 6.000, N'ABERTA', '2026-11-15T12:05:00'),
    (3, 2, 3, N'Entulho de obra bloqueando a calçada',
        N'Cenário fictício: entulho descartado obriga pedestres a andar pela rua.',
        N'Rua Fictícia das Árvores, 300 (Vila Mariana)', N'm3', 4.500, 4.500, N'ABERTA', '2026-11-15T12:10:00');
SET IDENTITY_INSERT OCORRENCIA OFF;

COMMIT TRANSACTION;

SELECT 'REGIAO' AS TABELA, COUNT(*) AS LINHAS FROM REGIAO
UNION ALL SELECT 'CATEGORIA_OCORRENCIA', COUNT(*) FROM CATEGORIA_OCORRENCIA
UNION ALL SELECT 'OCORRENCIA', COUNT(*) FROM OCORRENCIA
UNION ALL SELECT 'IMAGEM_OCORRENCIA', COUNT(*) FROM IMAGEM_OCORRENCIA
UNION ALL SELECT 'ATENDIMENTO_OCORRENCIA', COUNT(*) FROM ATENDIMENTO_OCORRENCIA;
-- Esperado: 2 regiões, 3 categorias, 3 ocorrências, 0 imagens, 0 atendimentos.
-- Linha de base da transação: ocorrência 1 com 20,000 m2 pendentes e 0 atendimentos.
