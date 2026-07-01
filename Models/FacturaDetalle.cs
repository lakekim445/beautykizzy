using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("FacturaDetalles")]
    public class FacturaDetalle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FacturaId { get; set; }

        [Required]
        public int ServicioId { get; set; }

        // Precio base del servicio en el momento de facturar
        [Required]
        [DataType(DataType.Currency)]
        public decimal PrecioUnitario { get; set; }

        // Cargo adicional por complejidad (ej. "diseño con adornos")
        [DataType(DataType.Currency)]
        public decimal CargosExtra { get; set; } = 0;

        [DataType(DataType.Currency)]
        public decimal Descuento { get; set; } = 0;

        [Required]
        [DataType(DataType.Currency)]
        public decimal Subtotal { get; set; }

        [StringLength(300)]
        public string? DescripcionExtra { get; set; } // ej. "Diseño con adornos"

        [ForeignKey("FacturaId")]
        public virtual Factura? Factura { get; set; }

        [ForeignKey("ServicioId")]
        public virtual Servicio? Servicio { get; set; }
    }
}
