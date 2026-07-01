using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [Required]
        [StringLength(20)]
        public string Rol { get; set; } = Roles.Cliente;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relación: si este usuario es un empleado, tiene un registro asociado
        public virtual Empleado? Empleado { get; set; }
    }
}
