using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Servicios")]
    public class Servicio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del servicio es requerido")]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es requerida")]
        public int CategoriaId { get; set; }

        [Required]
        [Range(5, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos")]
        public int DuracionMinutos { get; set; }

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        [DataType(DataType.Currency)]
        public decimal PrecioBase { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(500)]
        public string? ImagenUrl { get; set; }

        public bool Activo { get; set; } = true;

        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; }

        // Receta: qué productos consume este servicio
        public virtual ICollection<ServicioProducto>? ServicioProductos { get; set; }

        // Qué herramientas necesita este servicio
        public virtual ICollection<ServicioHerramienta>? ServicioHerramientas { get; set; }
    }
}
