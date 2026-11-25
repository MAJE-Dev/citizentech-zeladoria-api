using CitizenTech.Zeladoria.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CitizenTech.Zeladoria.Api.Controllers;

/// <summary>Consultas do domínio (GET, LINQ assíncrono) e registro de ocorrência pelo cidadão (POST JSON).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public sealed class OcorrenciasController(AppDb db, Equipe equipe, ILogger<OcorrenciasController> log) : ControladorBase
{
    private static readonly HashSet<string> Unidades = ["m2", "m3", "m", "un", "kg"];

    /// <summary>Identificação acadêmica local (não é autenticação).</summary>
    [HttpGet("equipe")]
    public IActionResult EquipeAtual() => Ok(equipe);

    [HttpGet("regioes")]
    public async Task<IActionResult> Regioes(CancellationToken ct) =>
        Ok(await db.Regioes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct));

    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias(CancellationToken ct) =>
        Ok(await db.Categorias.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct));

    /// <summary>Lista ocorrências com filtros opcionais. Não carrega bytes de imagem.</summary>
    [HttpGet("ocorrencias")]
    public async Task<IActionResult> Listar([FromQuery] int? regiaoId, [FromQuery] int? categoriaId,
        [FromQuery] string? situacao, CancellationToken ct)
    {
        var consulta = db.Ocorrencias.AsNoTracking();
        if (regiaoId is not null) consulta = consulta.Where(o => o.RegiaoId == regiaoId);
        if (categoriaId is not null) consulta = consulta.Where(o => o.CategoriaId == categoriaId);
        if (!string.IsNullOrWhiteSpace(situacao))
        {
            var s = situacao.Trim().ToUpperInvariant();
            consulta = consulta.Where(o => o.Situacao == s);
        }
        // Correção (E09_02): ordenar DENTRO de Projetar. OrderBy depois do construtor do record não é traduzível pelo EF Core.
        return Ok(await Projetar(consulta).ToListAsync(ct));
    }

    [HttpGet("ocorrencias/{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct)
    {
        var item = await Projetar(db.Ocorrencias.AsNoTracking().Where(o => o.Id == id)).SingleOrDefaultAsync(ct);
        return item is null ? Falha(404, "Ocorrência inexistente", $"Não existe ocorrência {id}.") : Ok(item);
    }

    [HttpGet("ocorrencias/{id:int}/atendimentos")]
    public async Task<IActionResult> Atendimentos(int id, CancellationToken ct)
    {
        if (!await db.Ocorrencias.AnyAsync(o => o.Id == id, ct))
            return Falha(404, "Ocorrência inexistente", $"Não existe ocorrência {id}.");
        return Ok(await db.Atendimentos.AsNoTracking().Where(a => a.OcorrenciaId == id).OrderBy(a => a.Id).ToListAsync(ct));
    }

    /// <summary>Metadados das imagens de uma ocorrência (sem os bytes).</summary>
    [HttpGet("ocorrencias/{id:int}/imagens")]
    public async Task<IActionResult> Imagens(int id, CancellationToken ct)
    {
        if (!await db.Ocorrencias.AnyAsync(o => o.Id == id, ct))
            return Falha(404, "Ocorrência inexistente", $"Não existe ocorrência {id}.");
        return Ok(await db.Imagens.AsNoTracking().Where(i => i.OcorrenciaId == id).OrderBy(i => i.Id)
            .Select(i => new ImagemMetadados(i.Id, i.OcorrenciaId, i.Titulo, i.NomeArquivo, i.Mime,
                i.TamanhoBytes, i.HashSha256, i.EquipeRm, i.Executor, i.EnviadaUtc))
            .ToListAsync(ct));
    }

    /// <summary>Cidadão registra um problema urbano (JSON). Começa ABERTA com pendente = estimada.</summary>
    [HttpPost("ocorrencias")]
    [Consumes("application/json")]
    public async Task<IActionResult> Criar(OcorrenciaEntrada e, CancellationToken ct)
    {
        if (!equipe.Contem(e.Executor))
            return Falha(400, "Executor inválido", "Executor não pertence à equipe (use NomeAluno_RM de equipe.json).");
        var titulo = e.Titulo?.Trim() ?? "";
        var descricao = e.Descricao?.Trim() ?? "";
        var endereco = e.Endereco?.Trim() ?? "";
        var unidade = e.Unidade?.Trim().ToLowerInvariant() ?? "";
        if (titulo.Length is 0 or > 100) return Falha(400, "Título inválido", "Título obrigatório, com até 100 caracteres.");
        if (descricao.Length is 0 or > 500) return Falha(400, "Descrição inválida", "Descrição obrigatória, com até 500 caracteres.");
        if (endereco.Length is 0 or > 150) return Falha(400, "Endereço inválido", "Endereço obrigatório, com até 150 caracteres.");
        if (!Unidades.Contains(unidade)) return Falha(400, "Unidade inválida", "Use uma unidade: m2, m3, m, un ou kg.");
        if (!QuantidadeValida(e.QuantidadeEstimada))
            return Falha(400, "Quantidade inválida", "Quantidade estimada deve ser positiva, com até 3 casas decimais.");
        if (!await db.Regioes.AnyAsync(r => r.Id == e.RegiaoId, ct))
            return Falha(400, "Região inexistente", $"Não existe região {e.RegiaoId}; nada foi gravado.");
        if (!await db.Categorias.AnyAsync(c => c.Id == e.CategoriaId, ct))
            return Falha(400, "Categoria inexistente", $"Não existe categoria {e.CategoriaId}; nada foi gravado.");

        var nova = new Ocorrencia
        {
            RegiaoId = e.RegiaoId, CategoriaId = e.CategoriaId, Titulo = titulo, Descricao = descricao,
            Endereco = endereco, Unidade = unidade, QuantidadeEstimada = e.QuantidadeEstimada,
            QuantidadePendente = e.QuantidadeEstimada, Situacao = Situacoes.Aberta, AbertaUtc = DateTime.UtcNow
        };
        db.Ocorrencias.Add(nova);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Ocorrência {Id} registrada por {Executor}", nova.Id, e.Executor);

        var criada = await Projetar(db.Ocorrencias.AsNoTracking().Where(o => o.Id == nova.Id)).SingleAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = nova.Id }, criada);
    }

    /// <summary>Projeção LINQ com JOIN em região/categoria e contagens (traduzida para um único SELECT ... ORDER BY).</summary>
    private IQueryable<OcorrenciaResumo> Projetar(IQueryable<Ocorrencia> consulta) =>
        from o in consulta
        join r in db.Regioes on o.RegiaoId equals r.Id
        join c in db.Categorias on o.CategoriaId equals c.Id
        orderby o.Id
        select new OcorrenciaResumo(o.Id, o.Titulo, r.Nome, r.Bairro, c.Nome, o.Endereco, o.Unidade,
            o.QuantidadeEstimada, o.QuantidadePendente, o.Situacao, o.AbertaUtc,
            db.Imagens.Count(i => i.OcorrenciaId == o.Id),
            db.Atendimentos.Count(a => a.OcorrenciaId == o.Id));
}
