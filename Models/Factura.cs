using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Facturas")]
    public class Factura
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int ReservaId { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido")]
        public int MetodoPagoId { get; set; }

        [Required]
        [StringLength(50)]
        public string NumeroFactura { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Currency)]
        public decimal Subtotal { get; set; }

        [DataType(DataType.Currency)]
        public decimal Descuento { get; set; } = 0;

        [DataType(DataType.Currency)]
        public decimal Iva { get; set; } = 0;

        [Required]
        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

        [ForeignKey("ClienteId")]
        public virtual Usuario? Cliente { get; set; }

        [ForeignKey("ReservaId")]
        public virtual Reserva? Reserva { get; set; }

        [ForeignKey("MetodoPagoId")]
        public virtual MetodoPago? MetodoPago { get; set; }

        public virtual ICollection<FacturaDetalle>? Detalles { get; set; }
    }
}
