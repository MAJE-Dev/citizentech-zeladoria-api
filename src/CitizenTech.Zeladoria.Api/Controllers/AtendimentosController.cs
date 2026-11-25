using CitizenTech.Zeladoria.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CitizenTech.Zeladoria.Api.Controllers;

/// <summary>
/// Operação transacional: registrar atendimento de campo.
/// Duas escritas atômicas na MESMA transação/conexão do DbContext:
///   (1) UPDATE OCORRENCIA: baixa do pendente e nova situação;
///   (2) INSERT ATENDIMENTO_OCORRENCIA: registro do serviço executado.
/// Baixa sem registro = serviço "fantasma"; registro sem baixa = pendência falsa e risco de atendimento duplicado.
/// </summary>
[ApiController]
[Route("api/ocorrencias/{ocorrenciaId:int}/atendimentos")]
public sealed class AtendimentosController(AppDb db, Equipe equipe, IWebHostEnvironment ambiente,
    ILogger<AtendimentosController> log) : ControladorBase
{
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> Registrar(int ocorrenciaId, AtendimentoEntrada entrada, CancellationToken ct)
    {
        // Validações ANTES da transação: rejeições aqui não escrevem nada (C6), mas não provam rollback (C5).
        if (!equipe.Contem(entrada.Executor))
            return Falha(400, "Executor inválido", "Executor não pertence à equipe (use NomeAluno_RM de equipe.json).");
        if (!QuantidadeValida(entrada.Quantidade))
            return Falha(400, "Quantidade inválida", "Quantidade deve ser positiva, com até 3 casas decimais.");
        var codigoEquipe = entrada.EquipeCampoCodigo?.Trim() ?? "";
        if (codigoEquipe.Length is 0 or > 40)
            return Falha(400, "Equipe de campo inválida", "Informe o código fictício da equipe de campo (até 40 caracteres).");
        if (entrada.SimularFalha && !ambiente.IsDevelopment())
            return Falha(400, "Simulação não permitida", "A falha didática só é aceita no ambiente Development.");
        if (!await db.Ocorrencias.AnyAsync(o => o.Id == ocorrenciaId, ct))
            return Falha(404, "Ocorrência inexistente", $"Não existe ocorrência {ocorrenciaId}; nada foi gravado.");

        var q = entrada.Quantidade;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        log.LogInformation("[TX] BEGIN | ocorrencia={Ocorrencia} quantidade={Quantidade} executor={Executor} simularFalha={Falha}",
            ocorrenciaId, q, entrada.Executor, entrada.SimularFalha);
        try
        {
            // Escrita 1: UPDATE condicional verifica e baixa o pendente numa única instrução
            // (protege contra duas chamadas concorrentes usando o mesmo saldo).
            var alteradas = await db.Ocorrencias
                .Where(o => o.Id == ocorrenciaId && o.Situacao != Situacoes.Concluida && o.QuantidadePendente >= q)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.QuantidadePendente, o => o.QuantidadePendente - q)
                    .SetProperty(o => o.Situacao, o => o.QuantidadePendente - q == 0 ? Situacoes.Concluida : Situacoes.EmAtendimento), ct);

            if (alteradas == 0)
            {
                await tx.RollbackAsync(ct);
                var atual = await db.Ocorrencias.AsNoTracking().Where(o => o.Id == ocorrenciaId)
                    .Select(o => new { o.QuantidadePendente, o.Unidade, o.Situacao }).SingleAsync(ct);
                log.LogInformation("[TX] ROLLBACK sem alteracoes | pendente={Pendente} situacao={Situacao}", atual.QuantidadePendente, atual.Situacao);
                return Falha(409, "Quantidade indisponível",
                    $"Pendente atual: {atual.QuantidadePendente} {atual.Unidade} (situação {atual.Situacao}). Nenhum atendimento foi gravado.");
            }
            log.LogInformation("[TX] escrita 1/2 OK: UPDATE OCORRENCIA {Ocorrencia} (1 linha)", ocorrenciaId);

            // Escrita 2: INSERT do atendimento na mesma transação.
            var atendimento = new AtendimentoOcorrencia
            {
                OcorrenciaId = ocorrenciaId, Quantidade = q, EquipeCampoCodigo = codigoEquipe,
                EquipeRm = equipe.LiderRm, Executor = entrada.Executor!, CriadoUtc = DateTime.UtcNow
            };
            db.Atendimentos.Add(atendimento);
            await db.SaveChangesAsync(ct);
            log.LogInformation("[TX] escrita 2/2 OK: INSERT ATENDIMENTO_OCORRENCIA ID={Id} (ainda NAO confirmado)", atendimento.Id);

            // Falha didática DEPOIS das duas escritas chegarem ao banco e ANTES do COMMIT.
            if (entrada.SimularFalha)
                throw new FalhaDemonstracaoException();

            await tx.CommitAsync(ct);
            log.LogInformation("[TX] COMMIT | atendimento={Id} confirmado junto com a baixa da ocorrencia {Ocorrencia}", atendimento.Id, ocorrenciaId);

            var depois = await db.Ocorrencias.AsNoTracking().Where(o => o.Id == ocorrenciaId)
                .Select(o => new { o.QuantidadePendente, o.Situacao }).SingleAsync(ct);
            return Created($"/api/ocorrencias/{ocorrenciaId}/atendimentos", new
            {
                resultado = "COMMIT",
                atendimento.Id, atendimento.OcorrenciaId, atendimento.Quantidade, atendimento.EquipeCampoCodigo,
                atendimento.EquipeRm, atendimento.Executor, atendimento.CriadoUtc,
                ocorrenciaPendente = depois.QuantidadePendente, ocorrenciaSituacao = depois.Situacao
            });
        }
        catch (FalhaDemonstracaoException)
        {
            await tx.RollbackAsync(CancellationToken.None);
            log.LogWarning("[TX] ROLLBACK | falha didatica apos UPDATE e INSERT; nada foi confirmado");
            return Conflict(new
            {
                resultado = "ROLLBACK",
                mensagem = "Falha didática após as duas escritas (UPDATE e INSERT). Nada foi confirmado."
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await tx.RollbackAsync(CancellationToken.None);
            log.LogError(ex, "[TX] ROLLBACK | falha inesperada na ocorrencia {Ocorrencia}", ocorrenciaId);
            return TratadorDeFalhas.BancoIndisponivel(ex)
                ? Falha(503, "Banco de dados indisponível", "Não foi possível concluir. As alterações foram revertidas.")
                : Falha(500, "Falha interna", "Não foi possível concluir. As alterações foram revertidas.");
        }
    }

    private sealed class FalhaDemonstracaoException : Exception;
}
