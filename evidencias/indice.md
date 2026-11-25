# Índice de evidências — E01 a E09

Zeladoria API Urbana · GS 2026.2 C# · 3ESPH · Líder KaioSouza_RM556095 · Banco: SQL Server 2022 Express LocalDB (`(localdb)\MSSQLLocalDB`, banco `GS_CSHARP_ZELADORIA`) · Execução: 25/11/2026, 19:56–19:58 (-03:00), por **VitorCardoso_RM556404** na máquina VITORPC.

Os arquivos `.txt` são a saída **real e não editada** do `testes/Executar-Roteiro-T01-T10.ps1` e do `sql/sqlserver/Executar-Sql.ps1`. Cada um registra o esperado, a chamada, a resposta HTTP completa, as consultas SQL antes/depois e a linha `=> Esperado … | Observado … | CONFERE/DIVERGE`. Os logs completos da API (SQL executado pelo EF Core e transações) ficam em `testes/saida/api_sessao1..5.log`.

| E | Arquivo(s) | O que comprova | T | R |
|---|---|---|---|---|
| E01 | `E01_01_Registro_Escuta_Mooca_2026-11-14.md`; `README.md` (ficha); `docs/relatorio.pdf` §2; `../amostras/ODS11_icone_oficial_ONU_ptBR.svg` | Cidadão, problema, território, escuta real (data, contexto, síntese, ligação), ODS 11, indicador e ecossistema | T01, T09 | R1, R2 |
| E02 | `E02_01_Ambiente_Versoes.txt` · `E02_02_SQL_00_Verificar_Ambiente.txt` · `E02_03_Build_e_Testes_Unitarios.txt` | Windows, SDK 10.0.404, runtime 10.0.15, EF Core SqlServer 10.0.15, SQL Server 16.0.1000.6, permissão no banco, build com 0 avisos e 11 testes unitários aprovados | T09 | R8, R9 |
| E03 | `E03_01_SQL_01_Criar_Estrutura.txt` · `E03_02_SQL_02_Carga_Ficticia.txt` · `E03_03_SQL_03_Linha_de_Base.txt` · `E03_04_Modelo_na_Ferramenta_diagrams.net.png` · `../modelo/` | 5 tabelas criadas, FKs `NO_ACTION`, 7 CHECKs, carga (2/3/3/0/0), linha de base 20,000/0 consistente; modelo aberto no diagrams.net com equipe e cardinalidades | T09 | R3 |
| E04 | `E04_01_T01_Consultas_e_Upload.txt` · `E04_02_T01_SQL_Bytes_no_Banco.txt` | GETs 200; POST ocorrência 201; upload pelo cliente C# com resposta 201 e **ID 100**; SQL: `DATALENGTH(DADOS)=52484` = arquivo; `HASHBYTES('SHA2_256')` = SHA-256 do original; metadados, `EQUIPERM` e `EXECUTOR` | T01 | R4, R5 |
| E05 | `E05_01_T02_Reinicio_e_Recuperacao.txt` · `E05_02_T02_Imagem_Exibida_Apos_Reinicio.png` | API parada (PID 22292) e reiniciada (PID 24984); o mesmo ID 100 é recuperado sem novo upload; hash recuperado = original; imagem exibida pelo Edge a partir de `GET /api/imagens/100/conteudo` | T02 | R4, R5 |
| E06 | `E06_01_T07_COMMIT.txt` | Antes 20,000/0 → chamada 201 `COMMIT` → depois 17,000/1 `EM_ATENDIMENTO`, com trecho do log: BEGIN, UPDATE, INSERT, COMMIT | T07 | R6 |
| E07 | `E07_01_T08_ROLLBACK.txt` | Antes 17,000/1 → falha **depois** do UPDATE e do INSERT (ID provisório 101 no log) → 409 `ROLLBACK` → depois 17,000/1 | T08 | R6 |
| E08 | `E08_01_T03_…` · `E08_02_T04_…` · `E08_03_T05_…` · `E08_04_T06_…` · `E08_05_T10_…` · `E08_06_Banco_Indisponivel_503.txt` · `E08_00_Resumo_Verificacoes_Automaticas.txt` | Rejeições 400/404/409/413/415 sem gravação; cliente trata a API parada (exit 2) e a retomada funciona; banco indisponível retorna 503 sem vazar detalhes; resumo 54/54 | T03, T04, T05, T06, T10 | R7, R8 |
| E09 | `E09_01_Mapa_Requisitos_e_Codigo.md` · `E09_02_Falha_Encontrada_e_Corrigida.txt` · `docs/relatorio.pdf` §3, §7, §9 | Mapa R1–R9 → arquivo/classe/método/linha; explicação de streams, JSON, HttpClient, async/await, EF/LINQ e transação; falha real encontrada, causa, correção e reexecução | T09 | R2–R9 |

## T → E (roteiro D02 §10)

| T | Procedimento | Evidência |
|---|---|---|
| T01 | Consultar equipe/ocorrências e enviar imagem válida vinculada ao domínio | E01, E04 |
| T02 | Parar/reiniciar a API e o cliente, recuperar pelo ID sem o original | E05 |
| T03 | Sem arquivo, 0 bytes, título vazio | E08_01 |
| T04 | GIF, PNG disfarçado, 2,5 MiB | E08_02 |
| T05 | ID ausente e vínculo inexistente | E08_03 |
| T06 | Quantidade 0/negativa/4 casas, saldo insuficiente, executor não cadastrado | E08_04 |
| T07 | COMMIT com antes/depois | E06 |
| T08 | ROLLBACK depois das escritas | E07 |
| T09 | Modelo na ferramenta + relatório/mapa | E03, E09 |
| T10 | API parada durante o cliente e retomada | E08_05 |
