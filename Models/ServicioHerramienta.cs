using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("ServicioHerramientas")]
    public class ServicioHerramienta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServicioId { get; set; }

        [Required]
        public int HerramientaId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CantidadNecesaria { get; set; } = 1;

        [ForeignKey("ServicioId")]
        public virtual Servicio? Servicio { get; set; }

        [ForeignKey("HerramientaId")]
        public virtual Herramienta? Herramienta { get; set; }
    }
}
