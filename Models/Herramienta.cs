using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Herramientas")]
    public class Herramienta
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la herramienta es requerido")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Debe haber al menos 1 unidad")]
        public int CantidadTotal { get; set; }

        // Se recalcula en base a las reservas EnCurso en el momento de la consulta;
        // no se edita manualmente desde un formulario.
        public int CantidadDisponible { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Bueno"; // Bueno | Regular | EnReparacion

        public bool Activo { get; set; } = true;
    }
}
