using System.ComponentModel.DataAnnotations;

namespace CitizenTech.Zeladoria.Models
{
    public class StatusOcorrencia
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string DescricaoStatus { get; set; } = string.Empty;
        public ICollection<Ocorrencia> Ocorrencias { get; set; } = new List<Ocorrencia>();
    }
}