using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Empleados")]
    public class Empleado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "La categoría/especialidad es requerida")]
        public int CategoriaId { get; set; }

        [StringLength(100)]
        public string? Especialidad { get; set; }

        [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0 y 100%")]
        public decimal ComisionPorcentaje { get; set; } = 20;

        public bool Activo { get; set; } = true;

        public DateTime FechaContratacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; }
    }
}
