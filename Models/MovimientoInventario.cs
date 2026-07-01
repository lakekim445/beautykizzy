using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    public enum TipoMovimiento
    {
        Entrada = 0, // compra de stock
        Salida = 1   // consumo por un servicio realizado
    }

    [Table("MovimientosInventario")]
    public class MovimientoInventario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public TipoMovimiento TipoMovimiento { get; set; }

        [Required]
        public decimal Cantidad { get; set; }

        // Si es una Salida automática por servicio completado, aquí va el Id de la Reserva.
        // Si es una Entrada manual, puede quedar null.
        public int? ReferenciaId { get; set; }

        public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? Observacion { get; set; }

        [ForeignKey("ProductoId")]
        public virtual Producto? Producto { get; set; }
    }
}
