using CitizenTech.Zeladoria.Data;
using CitizenTech.Zeladoria.DTOs;
using CitizenTech.Zeladoria.Models;
using Microsoft.EntityFrameworkCore;

namespace CitizenTech.Zeladoria.Services
{
    public class OcorrenciaService : IOcorrenciaService
    {
        private readonly AppDbContext _context;

        public OcorrenciaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OcorrenciaResponseDto>> ListarTodasAsync(int? categoriaId, int? statusId)
        {
            var query = _context.Ocorrencias
                .Include(o => o.Categoria)
                .Include(o => o.StatusOcorrencia)
                .Include(o => o.Usuario)
                .AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(o => o.CategoriaId == categoriaId.Value);
            }

            if (statusId.HasValue)
            {
                query = query.Where(o => o.StatusId == statusId.Value);
            }

            var ocorrencias = await query
                .OrderByDescending(o => o.DataRegistro)
                .ToListAsync();

            return ocorrencias.Select(MapearParaDto);
        }

        public async Task<OcorrenciaResponseDto?> ObterPorIdAsync(int id)
        {
            var ocorrencia = await _context.Ocorrencias
                .Include(o => o.Categoria)
                .Include(o => o.StatusOcorrencia)
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.Id == id);

            return ocorrencia is null ? null : MapearParaDto(ocorrencia);
        }

        public async Task<OcorrenciaResponseDto> CriarAsync(CriarOcorrenciaDto dto)
        {
            string? imagemUrl = null;

            // Tratamento do upload de imagem
            if (dto.Imagem != null && dto.Imagem.Length > 0)
            {
                var pastaUploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(pastaUploads))
                {
                    Directory.CreateDirectory(pastaUploads);
                }

                var nomeFicheiro = Guid.NewGuid().ToString() + Path.GetExtension(dto.Imagem.FileName);
                var caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiro);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await dto.Imagem.CopyToAsync(stream);
                }

                imagemUrl = $"/uploads/{nomeFicheiro}";
            }

            var novaOcorrencia = new Ocorrencia
            {
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                Endereco = dto.Endereco,
                CategoriaId = dto.CategoriaId,
                StatusId = 1,
                UsuarioId = dto.UsuarioId > 0 ? dto.UsuarioId : 1,
                ImagemUrl = imagemUrl
            };

            _context.Ocorrencias.Add(novaOcorrencia);
            await _context.SaveChangesAsync();

            // Busca os dados relacionados de forma segura para evitar erros de referência nula no In-Memory
            var categoria = await _context.Categorias.FindAsync(novaOcorrencia.CategoriaId);
            var status = await _context.StatusOcorrencias.FindAsync(novaOcorrencia.StatusId);
            var usuario = await _context.Usuarios.FindAsync(novaOcorrencia.UsuarioId);

            return new OcorrenciaResponseDto(
                novaOcorrencia.Id,
                novaOcorrencia.Titulo,
                novaOcorrencia.Descricao,
                categoria?.Nome ?? "Sem Categoria",
                novaOcorrencia.Endereco,
                status?.DescricaoStatus ?? "Aberta",
                novaOcorrencia.DataRegistro,
                usuario?.Nome ?? "Anônimo",
                novaOcorrencia.ImagemUrl
            );
        }

        // Novo método transacional atômico para o Critério C5 (Commit / Rollback) com suporte a imagem (C4)
        public async Task<OcorrenciaResponseDto> CriarComTransacaoAsync(CriarOcorrenciaComComentarioDto dto, bool simularErro = false)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string? imagemUrl = null;

                // Tratamento do upload de imagem na transação (C4)
                if (dto.Imagem != null && dto.Imagem.Length > 0)
                {
                    var pastaUploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    if (!Directory.Exists(pastaUploads))
                    {
                        Directory.CreateDirectory(pastaUploads);
                    }

                    var nomeFicheiro = Guid.NewGuid().ToString() + Path.GetExtension(dto.Imagem.FileName);
                    var caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiro);

                    using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await dto.Imagem.CopyToAsync(stream);
                    }

                    imagemUrl = $"/uploads/{nomeFicheiro}";
                }

                // Criar a Ocorrência
                var novaOcorrencia = new Ocorrencia
                {
                    Titulo = dto.Titulo,
                    Descricao = dto.Descricao,
                    Endereco = dto.Endereco,
                    CategoriaId = dto.CategoriaId,
                    StatusId = 1,
                    UsuarioId = dto.UsuarioId > 0 ? dto.UsuarioId : 1,
                    DataRegistro = DateTime.UtcNow,
                    ImagemUrl = imagemUrl
                };

                _context.Ocorrencias.Add(novaOcorrencia);
                await _context.SaveChangesAsync();

                // Simulação de falha
                if (simularErro)
                {
                    throw new InvalidOperationException("Erro simulado propositadamente para testar o Rollback da transação!");
                }

                var novoComentario = new Comentario
                {
                    Texto = dto.TextoComentario,
                    OcorrenciaId = novaOcorrencia.Id,
                    UsuarioId = dto.UsuarioId > 0 ? dto.UsuarioId : 1
                };

                _context.Comentarios.Add(novoComentario);
                await _context.SaveChangesAsync();

                // Se tudo correu bem, consolida as alterações
                await transaction.CommitAsync();

                var categoria = await _context.Categorias.FindAsync(novaOcorrencia.CategoriaId);
                var status = await _context.StatusOcorrencias.FindAsync(novaOcorrencia.StatusId);
                var usuario = await _context.Usuarios.FindAsync(novaOcorrencia.UsuarioId);

                return new OcorrenciaResponseDto(
                    novaOcorrencia.Id,
                    novaOcorrencia.Titulo,
                    novaOcorrencia.Descricao,
                    categoria?.Nome ?? "Sem Categoria",
                    novaOcorrencia.Endereco,
                    status?.DescricaoStatus ?? "Aberta",
                    novaOcorrencia.DataRegistro,
                    usuario?.Nome ?? "Anônimo",
                    novaOcorrencia.ImagemUrl
                );
            }
            catch (Exception)
            {
                // Se houver qualquer falha, desfaz todas as alterações
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OcorrenciaResponseDto?> AtualizarStatusAsync(int id, int novoStatusId)
        {
            var ocorrencia = await _context.Ocorrencias.FindAsync(id);
            if (ocorrencia is null)
            {
                return null;
            }

            ocorrencia.StatusId = novoStatusId;
            await _context.SaveChangesAsync();

            return await ObterPorIdAsync(ocorrencia.Id);
        }

        public async Task<bool> RemoverAsync(int id)
        {
            var ocorrencia = await _context.Ocorrencias.FindAsync(id);
            if (ocorrencia is null)
            {
                return false;
            }

            _context.Ocorrencias.Remove(ocorrencia);
            await _context.SaveChangesAsync();
            return true;
        }

        private static OcorrenciaResponseDto MapearParaDto(Ocorrencia o) =>
            new(
                o.Id,
                o.Titulo,
                o.Descricao,
                o.Categoria?.Nome ?? "Sem Categoria",
                o.Endereco,
                o.StatusOcorrencia?.DescricaoStatus ?? "Desconhecido",
                o.DataRegistro,
                o.Usuario?.Nome ?? "Anônimo",
                o.ImagemUrl
            );
    }
}