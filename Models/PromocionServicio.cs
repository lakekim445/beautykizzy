using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("PromocionServicios")]
    public class PromocionServicio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PromocionId { get; set; }

        [Required]
        public int ServicioId { get; set; }

        [ForeignKey("PromocionId")]
        public virtual Promocion? Promocion { get; set; }

        [ForeignKey("ServicioId")]
        public virtual Servicio? Servicio { get; set; }
    }
}
