using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    // ViewModel para el formulario de agendar (Cliente)
    public class AgendarViewModel
    {
        public int ServicioId { get; set; }
        public int? EmpleadoId { get; set; } // null = "cualquier empleado disponible"
        public DateTime Fecha { get; set; }
        public string? HoraSeleccionada { get; set; } // "14:30"
        public string? Notas { get; set; }

        public List<Servicio> Servicios { get; set; } = new();
        public List<Empleado> EmpleadosDelServicio { get; set; } = new();
    }

    public class ReservasController : BaseController
    {
        private readonly ApplicationDbContext _context;

        private static readonly TimeSpan HoraApertura = new(8, 0, 0);
        private static readonly TimeSpan HoraCierre = new(21, 0, 0);
        private static readonly TimeSpan InicioDescanso = new(12, 0, 0);
        private static readonly TimeSpan FinDescanso = new(13, 0, 0);

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Agendar()
        {
            if (!UsuarioLogueado()) return RedirectToAction("Login", "Account");

            var vm = new AgendarViewModel
            {
                Fecha = DateTime.Today,
                Servicios = await _context.Servicios.Where(s => s.Activo).ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EmpleadosPorServicio(int servicioId)
        {
            var servicio = await _context.Servicios.FindAsync(servicioId);
            if (servicio == null) return Json(new List<object>());



            var empleados = await _context.Empleados
                .Include(e => e.Usuario)
                .Where(e => e.Activo && e.CategoriaId == servicio.CategoriaId)
                .Select(e => new { id = e.Id, nombre = e.Usuario!.Nombre })
                .ToListAsync();

            return Json(empleados);
        }

        [HttpGet]
        public async Task<IActionResult> HorariosDisponibles(int servicioId, int? empleadoId, DateTime fecha)
        {
            var servicio = await _context.Servicios.FindAsync(servicioId);
            if (servicio == null)
                return Json(new List<string>());

            // No trabajamos los domingos
            if (fecha.DayOfWeek == DayOfWeek.Sunday)
                return Json(new List<string>());

            // Solo se pueden reservar citas hasta 3 meses adelante
            if (fecha.Date > DateTime.Today.AddMonths(3))
                return Json(new List<string>());

            var duracion = TimeSpan.FromMinutes(servicio.DuracionMinutos);

            var empleadosAConsiderar = empleadoId.HasValue
                ? new List<int> { empleadoId.Value }
                : await _context.Empleados
                    .Where(e => e.Activo && e.CategoriaId == servicio.CategoriaId)
                    .Select(e => e.Id)
                    .ToListAsync();

            if (!empleadosAConsiderar.Any())
                return Json(new List<string>());

            var inicioDia = fecha.Date;
            var finDia = fecha.Date.AddDays(1);

            var reservasDelDia = await _context.Reservas
                .Where(r => empleadosAConsiderar.Contains(r.EmpleadoId)
                    && r.FechaHora >= inicioDia && r.FechaHora < finDia
                    && r.Estado != EstadoReserva.Cancelada)
                .Include(r => r.Servicio)
                .ToListAsync();

            var horariosLibres = new List<string>();

            for (var hora = HoraApertura; hora + duracion <= HoraCierre; hora += TimeSpan.FromMinutes(30))
            {
                var inicioSlot = fecha.Date + hora;

                // Si la reserva es para hoy, no mostrar horas que ya pasaron
                if (fecha.Date == DateTime.Today && inicioSlot <= DateTime.Now)
                    continue;

                // Descanso de 12:00 a 13:00
                if (hora < FinDescanso && hora + duracion > InicioDescanso)
                    continue;

                var finSlot = inicioSlot + duracion;

                // ¿Hay al menos un empleado de la categoría libre en este horario?
                bool algunoLibre = empleadosAConsiderar.Any(empId =>
                {
                    var ocupado = reservasDelDia.Any(r =>
                    {
                        if (r.EmpleadoId != empId) return false;
                        var inicioExistente = r.FechaHora;
                        var finExistente = r.FechaHora.AddMinutes(r.Servicio?.DuracionMinutos ?? 0);
                        // Choque de horario: se solapan los intervalos
                        return inicioSlot < finExistente && finSlot > inicioExistente;
                    });
                    return !ocupado;
                });

                if (algunoLibre)
                {
                    horariosLibres.Add(hora.ToString(@"hh\:mm"));
                }
            }

            return Json(horariosLibres);
        }

        // POST: Reservas/Agendar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agendar(AgendarViewModel vm)
        {
            if (!UsuarioLogueado()) return RedirectToAction("Login", "Account");

            var servicio = await _context.Servicios.FindAsync(vm.ServicioId);
            if (servicio == null || string.IsNullOrEmpty(vm.HoraSeleccionada))
            {
                ModelState.AddModelError("", "Datos de la reserva incompletos");
            }

            if (!ModelState.IsValid || servicio == null)
            {
                vm.Servicios = await _context.Servicios.Where(s => s.Activo).ToListAsync();
                return View(vm);
            }

            var hora = TimeSpan.Parse(vm.HoraSeleccionada!);
            var fechaHora = vm.Fecha.Date + hora;

            // No permitir reservas los domingos
            if (vm.Fecha.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("", "El salón no atiende los domingos.");
            }

            // No permitir reservar con más de 3 meses de anticipación
            if (vm.Fecha.Date > DateTime.Today.AddMonths(3))
            {
                ModelState.AddModelError("", "Solo puedes reservar con hasta 3 meses de anticipación.");
            }

            // No permitir horarios fuera de atención
            if (hora < HoraApertura || hora >= HoraCierre)
            {
                ModelState.AddModelError("", "El horario seleccionado está fuera del horario de atención.");
            }

            // No permitir reservas durante el descanso
            if (hora >= InicioDescanso && hora < FinDescanso)
            {
                ModelState.AddModelError("", "El salón permanece cerrado de 12:00 a 13:00.");
            }

            // El servicio no puede terminar después de las 21:00
            if (fechaHora.AddMinutes(servicio.DuracionMinutos) > vm.Fecha.Date + HoraCierre)
            {
                ModelState.AddModelError("", "El servicio termina después del horario de atención.");
            }

            // ← ESTE BLOQUE TE FALTA
            if (!ModelState.IsValid)
            {
                vm.Servicios = await _context.Servicios
                    .Where(s => s.Activo)
                    .ToListAsync();

                return View(vm);
            }
            // Si el cliente no eligió empleado específico ("cualquiera disponible"),
            // asignamos el primero de la categoría que esté libre en ese horario exacto.
            int empleadoIdFinal;
            if (vm.EmpleadoId.HasValue)
            {
                // Regla de negocio dura: el empleado elegido DEBE ser de la categoría del servicio,
                // sin importar lo que el formulario haya mandado (por si alguien manipula la petición).
                var empleadoValido = await _context.Empleados
                    .AnyAsync(e => e.Id == vm.EmpleadoId.Value && e.CategoriaId == servicio.CategoriaId && e.Activo);

                if (!empleadoValido)
                {
                    ModelState.AddModelError("", "El empleado seleccionado no corresponde a la categoría de este servicio");
                    vm.Servicios = await _context.Servicios.Where(s => s.Activo).ToListAsync();
                    return View(vm);
                }
                empleadoIdFinal = vm.EmpleadoId.Value;
            }
            else
            {
                var candidatos = await _context.Empleados
                    .Where(e => e.Activo && e.CategoriaId == servicio.CategoriaId)
                    .Select(e => e.Id)
                    .ToListAsync();

                var finSlot = fechaHora.AddMinutes(servicio.DuracionMinutos);
                int? libre = null;
                foreach (var empId in candidatos)
                {
                    var choca = await _context.Reservas.AnyAsync(r =>
                        r.EmpleadoId == empId
                        && r.Estado != EstadoReserva.Cancelada
                        && r.FechaHora < finSlot
                        && r.FechaHora.AddMinutes(servicio.DuracionMinutos) > fechaHora);

                    if (!choca) { libre = empId; break; }
                }

                if (libre == null)
                {
                    ModelState.AddModelError("", "No hay empleados disponibles en ese horario, intenta otra hora");
                    vm.Servicios = await _context.Servicios.Where(s => s.Activo).ToListAsync();
                    return View(vm);
                }
                empleadoIdFinal = libre.Value;
            }

            // Última validación de choque, justo antes de guardar (por seguridad ante
            // dos personas reservando casi al mismo tiempo).
            var finNuevaReserva = fechaHora.AddMinutes(servicio.DuracionMinutos);
            var hayChoque = await _context.Reservas.AnyAsync(r =>
                r.EmpleadoId == empleadoIdFinal
                && r.Estado != EstadoReserva.Cancelada
                && r.FechaHora < finNuevaReserva
                && r.FechaHora.AddMinutes(servicio.DuracionMinutos) > fechaHora);

            if (hayChoque)
            {
                ModelState.AddModelError("", "Ese horario ya no está disponible, por favor elige otro");
                vm.Servicios = await _context.Servicios.Where(s => s.Activo).ToListAsync();
                return View(vm);
            }

            var reserva = new Reserva
            {
                ClienteId = GetUsuarioId(),
                EmpleadoId = empleadoIdFinal,
                ServicioId = servicio.Id,
                FechaHora = DateTime.SpecifyKind(fechaHora, DateTimeKind.Utc),
                Notas = vm.Notas,
                Estado = EstadoReserva.Pendiente,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            // Notificamos al empleado de su nueva asignación
            var empleadoAfectado = await _context.Empleados.FindAsync(empleadoIdFinal);
            if (empleadoAfectado != null)
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = empleadoAfectado.UsuarioId,
                    Titulo = "Nueva reserva asignada",
                    Mensaje = $"Tienes una nueva reserva de {servicio.Nombre} el {fechaHora:dd/MM/yyyy} a las {fechaHora:HH:mm}",
                    Tipo = TipoNotificacion.NuevaAsignacion
                });
                await _context.SaveChangesAsync();
            }

            TempData["Mensaje"] = "¡Reserva confirmada exitosamente!";
            return RedirectToAction(nameof(MisReservas));
        }

        // =====================================================
        // CLIENTE: Ver sus propias reservas
        // =====================================================
        public async Task<IActionResult> MisReservas()
        {
            if (!UsuarioLogueado()) return RedirectToAction("Login", "Account");

            var reservas = await _context.Reservas
                .Include(r => r.Servicio)
                .Include(r => r.Empleado!).ThenInclude(e => e.Usuario)
                .Where(r => r.ClienteId == GetUsuarioId())
                .OrderByDescending(r => r.FechaHora)
                .ToListAsync();

            return View(reservas);
        }

        // POST: Reservas/Cancelar/5 (el cliente puede cancelar su propia reserva pendiente)
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (!UsuarioLogueado()) return RedirectToAction("Login", "Account");

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id && r.ClienteId == GetUsuarioId());

            if (reserva != null && reserva.Estado is EstadoReserva.Pendiente or EstadoReserva.Confirmada)
            {
                reserva.Estado = EstadoReserva.Cancelada;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Reserva cancelada";
            }

            return RedirectToAction(nameof(MisReservas));
        }

        // =====================================================
        // EMPLEADO: Ver su propia agenda
        // =====================================================
        public async Task<IActionResult> MiAgenda()
        {
            if (GetRol() != Roles.Empleado) return RedirectToAction("Login", "Account");

            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.UsuarioId == GetUsuarioId());
            if (empleado == null) return RedirectToAction("Login", "Account");

            var reservas = await _context.Reservas
                .Include(r => r.Servicio)
                .Include(r => r.Cliente)
                .Where(r => r.EmpleadoId == empleado.Id && r.Estado != EstadoReserva.Cancelada)
                .OrderBy(r => r.FechaHora)
                .ToListAsync();

            return View(reservas);
        }

        // POST: Reservas/MarcarEnCurso/5 (empleado inicia el servicio)
        [HttpPost]
        public async Task<IActionResult> MarcarEnCurso(int id)
        {
            if (GetRol() != Roles.Empleado) return RedirectToAction("Login", "Account");

            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva != null && reserva.Estado == EstadoReserva.Confirmada)
            {
                reserva.Estado = EstadoReserva.EnCurso;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MiAgenda));
        }

        // POST: Reservas/Confirmar/5 (empleado o admin confirma una reserva pendiente)
        [HttpPost]
        public async Task<IActionResult> Confirmar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva != null && reserva.Estado == EstadoReserva.Pendiente)
            {
                reserva.Estado = EstadoReserva.Confirmada;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MiAgenda));
        }

        // =====================================================
        // GERENTE/ADMIN: Ver todas las reservas del salón
        // =====================================================
        public async Task<IActionResult> Index()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            var reservas = await _context.Reservas
                .Include(r => r.Servicio)
                .Include(r => r.Cliente)
                .Include(r => r.Empleado!).ThenInclude(e => e.Usuario)
                .OrderByDescending(r => r.FechaHora)
                .ToListAsync();

            return View(reservas);
        }
    }
}
