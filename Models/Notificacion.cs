using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    public static class TipoNotificacion
    {
        public const string RecordatorioCita = "RecordatorioCita";
        public const string StockBajo = "StockBajo";
        public const string NuevaAsignacion = "NuevaAsignacion";
    }

    [Table("Notificaciones")]
    public class Notificacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = string.Empty;

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaLectura { get; set; }

        [StringLength(200)]
        public string? EnlaceAccion { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }
    }
}
