using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyKizzy.Models
{
    // Representado como int en la base de datos (0,1,2,3,4) para que coincida
    // exactamente con el campo "Estado" de tipo int del modelo Reserva.
    public enum EstadoReserva
    {
        Pendiente = 0,
        Confirmada = 1,
        EnCurso = 2,
        Completada = 3,
        Cancelada = 4
    }

    [Table("Reservas")]
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        public int ServicioId { get; set; }

        public int? PromocionId { get; set; }

        [Required(ErrorMessage = "La fecha y hora son requeridas")]
        public DateTime FechaHora { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [ForeignKey("ClienteId")]
        public virtual Usuario? Cliente { get; set; }

        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }

        [ForeignKey("ServicioId")]
        public virtual Servicio? Servicio { get; set; }

        [ForeignKey("PromocionId")]
        public virtual Promocion? Promocion { get; set; }

        public virtual Factura? Factura { get; set; }
    }
}
