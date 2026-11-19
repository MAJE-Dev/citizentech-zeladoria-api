using System.ComponentModel.DataAnnotations;

namespace CitizenTech.Zeladoria.Models
{
    public class Comentario
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Texto { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public int OcorrenciaId { get; set; }
        public Ocorrencia? Ocorrencia { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }
}