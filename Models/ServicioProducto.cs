using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("ServicioProductos")]
    public class ServicioProducto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServicioId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        [Range(0.01, 9999.99)]
        public decimal Cantidad { get; set; }

        [ForeignKey("ServicioId")]
        public virtual Servicio? Servicio { get; set; }

        [ForeignKey("ProductoId")]
        public virtual Producto? Producto { get; set; }
    }
}
