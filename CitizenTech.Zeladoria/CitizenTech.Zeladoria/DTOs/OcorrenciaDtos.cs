using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CitizenTech.Zeladoria.DTOs
{
    public class CriarOcorrenciaDto
    {
        [Required(ErrorMessage = "O título da ocorrência é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 100 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O endereço é obrigatório.")]
        public string Endereco { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "O CategoriaId deve ser válido.")]
        public int CategoriaId { get; set; }

        public int UsuarioId { get; set; }

        public IFormFile? Imagem { get; set; }
    }

    public class AtualizarStatusDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "O ID do status deve ser válido.")]
        public int NovoStatusId { get; set; }
    }

    // DTO para suportar a operação transacional com validações
    public record CriarOcorrenciaComComentarioDto(
        [Required(ErrorMessage = "O título é obrigatório.")]
        string Titulo,
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        string Descricao,
        [Required(ErrorMessage = "O endereço é obrigatório.")]
        string Endereco,
        int CategoriaId,
        int UsuarioId,
        [Required(ErrorMessage = "O texto do comentário inicial é obrigatório.")]
        string TextoComentario,
        IFormFile? Imagem
    );

    public record OcorrenciaResponseDto(
        int Id,
        string Titulo,
        string Descricao,
        string CategoriaNome,
        string Endereco,
        string StatusDescricao,
        DateTime DataRegistro,
        string UsuarioNome,
        string? ImagemUrl
    );
}