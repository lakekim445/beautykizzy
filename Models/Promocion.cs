using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Promociones")]
    public class Promocion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la promoción es requerido")]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal PrecioPromocion { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        public bool Activa { get; set; } = true;

        public virtual ICollection<PromocionServicio>? PromocionServicios { get; set; }
    }
}
