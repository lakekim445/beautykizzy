using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La unidad de medida es requerida")]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = string.Empty;

        public decimal StockActual { get; set; } = 0;

        public decimal StockMinimo { get; set; } = 5;

        [Range(0, 999999.99)]
        [DataType(DataType.Currency)]
        public decimal? CostoUnitario { get; set; }

        public int? ProveedorId { get; set; }

        public bool Activo { get; set; } = true;

        [ForeignKey("ProveedorId")]
        public virtual Proveedor? Proveedor { get; set; }
    }
}
