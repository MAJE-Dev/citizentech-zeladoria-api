using System.ComponentModel.DataAnnotations;

namespace CitizenTech.Zeladoria.Models
{
    public class Ocorrencia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        public string Endereco { get; set; } = string.Empty;

        public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

        // Novo campo para persistir o caminho/URL da imagem associada (C4)
        public string? ImagemUrl { get; set; }

        // Chaves Estrangeiras
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        public int StatusId { get; set; }
        public StatusOcorrencia? StatusOcorrencia { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
    }
}