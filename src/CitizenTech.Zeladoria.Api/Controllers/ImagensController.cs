using System.Security.Cryptography;
using CitizenTech.Zeladoria.Api.Dados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CitizenTech.Zeladoria.Api.Controllers;

/// <summary>
/// Imagem persistente: upload multipart -> stream -> byte[] -> varbinary(max) no SQL Server.
/// Metadados em JSON (sem bytes) e conteúdo binário recuperado do banco por ID.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ImagensController(AppDb db, Equipe equipe, ILogger<ImagensController> log) : ControladorBase
{
    [HttpPost("ocorrencias/{ocorrenciaId:int}/imagens")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Enviar(int ocorrenciaId, [FromForm] UploadImagem entrada, CancellationToken ct)
    {
        // 1) Validações baratas antes de ler o arquivo (nenhuma gravação acontece se falharem).
        if (!equipe.Contem(entrada.Executor))
            return Falha(400, "Executor inválido", "Executor não pertence à equipe (use NomeAluno_RM de equipe.json).");
        var titulo = entrada.Titulo?.Trim() ?? "";
        if (titulo.Length is 0 or > 100)
            return Falha(400, "Título inválido", "Título obrigatório, com até 100 caracteres.");
        var arquivo = entrada.Arquivo;
        if (arquivo is null || arquivo.Length == 0)
            return Falha(400, "Arquivo ausente ou vazio", "Envie uma imagem PNG/JPEG não vazia no campo Arquivo.");
        if (arquivo.Length > ValidadorImagem.MaxBytes)
            return Falha(413, "Imagem muito grande", $"Limite de 2 MiB ({ValidadorImagem.MaxBytes} bytes) por imagem; recebido {arquivo.Length} bytes.");
        var nome = ValidadorImagem.NomeSeguro(arquivo.FileName);
        if (nome.Length is 0 or > 200)
            return Falha(400, "Nome de arquivo inválido", "O nome do arquivo deve ter de 1 a 200 caracteres.");
        if (!await db.Ocorrencias.AnyAsync(o => o.Id == ocorrenciaId, ct))
            return Falha(404, "Ocorrência inexistente", $"Não existe ocorrência {ocorrenciaId}; nenhuma imagem foi gravada.");

        // 2) Leitura ASSÍNCRONA do stream do upload para bytes (tamanho já limitado a 2 MiB).
        byte[] bytes;
        await using (var origem = arquivo.OpenReadStream())
        {
            using var memoria = new MemoryStream(capacity: (int)arquivo.Length);
            await origem.CopyToAsync(memoria, ct);
            bytes = memoria.ToArray();
        }

        // 3) Coerência extensão + Content-Type + assinatura (PNG/JPEG). Extensão sozinha não basta.
        if (!ValidadorImagem.FormatoCoerente(nome, arquivo.ContentType, bytes, out var mime))
            return Falha(415, "Formato não aceito",
                "Envie PNG ou JPEG com extensão, Content-Type e assinatura (bytes iniciais) compatíveis.");

        // 4) Bytes reais + metadados técnicos no SQL Server. O ID só é devolvido após o SaveChangesAsync.
        var imagem = new ImagemOcorrencia
        {
            OcorrenciaId = ocorrenciaId, Titulo = titulo, NomeArquivo = nome, Mime = mime,
            TamanhoBytes = bytes.Length, HashSha256 = Convert.ToHexString(SHA256.HashData(bytes)), Dados = bytes,
            EquipeRm = equipe.LiderRm, Executor = entrada.Executor!, EnviadaUtc = DateTime.UtcNow
        };
        db.Imagens.Add(imagem);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Imagem {Id} gravada em IMAGEM_OCORRENCIA | ocorrencia={Ocorrencia} bytes={Bytes} sha256={Hash} executor={Executor}",
            imagem.Id, ocorrenciaId, imagem.TamanhoBytes, imagem.HashSha256, imagem.Executor);

        return CreatedAtAction(nameof(Metadados), new { id = imagem.Id }, new ImagemMetadados(imagem.Id, imagem.OcorrenciaId,
            imagem.Titulo, imagem.NomeArquivo, imagem.Mime, imagem.TamanhoBytes, imagem.HashSha256,
            imagem.EquipeRm, imagem.Executor, imagem.EnviadaUtc));
    }

    /// <summary>Metadados técnicos por ID (projeção LINQ sem carregar a coluna DADOS).</summary>
    [HttpGet("imagens/{id:int}")]
    [Produces("application/json")]
    public async Task<IActionResult> Metadados(int id, CancellationToken ct)
    {
        var item = await db.Imagens.AsNoTracking().Where(i => i.Id == id)
            .Select(i => new ImagemMetadados(i.Id, i.OcorrenciaId, i.Titulo, i.NomeArquivo, i.Mime,
                i.TamanhoBytes, i.HashSha256, i.EquipeRm, i.Executor, i.EnviadaUtc))
            .SingleOrDefaultAsync(ct);
        return item is null ? Falha(404, "Imagem inexistente", $"Não existe imagem {id}.") : Ok(item);
    }

    /// <summary>Conteúdo binário lido do banco (não de pasta local), com o MIME gravado.</summary>
    [HttpGet("imagens/{id:int}/conteudo")]
    public async Task<IActionResult> Conteudo(int id, CancellationToken ct)
    {
        var item = await db.Imagens.AsNoTracking().Where(i => i.Id == id)
            .Select(i => new { i.Dados, i.Mime, i.HashSha256 })
            .SingleOrDefaultAsync(ct);
        if (item is null) return Falha(404, "Imagem inexistente", $"Não existe imagem {id}.");

        Response.Headers["X-Origem-Imagem"] = $"SQL Server IMAGEM_OCORRENCIA ID={id}";
        Response.Headers["X-Imagem-SHA256"] = item.HashSha256;
        return File(item.Dados, item.Mime); // o corpo é escrito de forma assíncrona no stream da resposta
    }
}
