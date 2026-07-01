using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class ServicioTop
    {
        public string Nombre { get; set; } = string.Empty;
        public int VecesRealizado { get; set; }
        public decimal TotalRecaudado { get; set; }
    }

    public class EmpleadoTop
    {
        public string Nombre { get; set; } = string.Empty;
        public int ServiciosRealizados { get; set; }
        public decimal TotalGenerado { get; set; }
        public decimal ComisionGanada { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalReservasHoy { get; set; }
        public decimal VentasMesActual { get; set; }
        public decimal VentasHoy { get; set; }
        public int ProductosBajoStock { get; set; }
        public decimal CrecimientoPorcentual { get; set; }
        public List<ServicioTop> ServiciosMasVendidos { get; set; } = new();
        public List<EmpleadoTop> EmpleadosTop { get; set; } = new();
    }

    public class ReportesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Reportes/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            var vm = new DashboardViewModel();

            var hoyInicio = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var hoyFin = hoyInicio.AddDays(1);
            var primerDiaMes = DateTime.SpecifyKind(
                new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), DateTimeKind.Utc);

            vm.TotalReservasHoy = await _context.Reservas
                .CountAsync(r => r.FechaHora >= hoyInicio && r.FechaHora < hoyFin && r.Estado != EstadoReserva.Cancelada);

            vm.VentasHoy = await _context.Facturas
                .Where(f => f.FechaEmision >= hoyInicio && f.FechaEmision < hoyFin)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            vm.VentasMesActual = await _context.Facturas
                .Where(f => f.FechaEmision >= primerDiaMes)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            vm.ProductosBajoStock = await _context.Productos
                .CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo);

            // Crecimiento respecto al mes anterior
            var mesAnterior = DateTime.UtcNow.AddMonths(-1);
            var primerDiaMesAnterior = DateTime.SpecifyKind(
                new DateTime(mesAnterior.Year, mesAnterior.Month, 1), DateTimeKind.Utc);
            var ultimoDiaMesAnterior = primerDiaMes;

            var ventasMesAnterior = await _context.Facturas
                .Where(f => f.FechaEmision >= primerDiaMesAnterior && f.FechaEmision < ultimoDiaMesAnterior)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            vm.CrecimientoPorcentual = ventasMesAnterior > 0
                ? ((vm.VentasMesActual - ventasMesAnterior) / ventasMesAnterior) * 100
                : (vm.VentasMesActual > 0 ? 100 : 0);

            // Servicios más vendidos (por número de facturas que los incluyen)
            vm.ServiciosMasVendidos = await _context.FacturaDetalles
                .Include(d => d.Servicio)
                .GroupBy(d => d.ServicioId)
                .Select(g => new ServicioTop
                {
                    Nombre = g.First().Servicio!.Nombre,
                    VecesRealizado = g.Count(),
                    TotalRecaudado = g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(s => s.VecesRealizado)
                .Take(5)
                .ToListAsync();

            // Top empleados por servicios completados y comisión generada
            var facturasConDetalle = await _context.Facturas
                .Include(f => f.Reserva!).ThenInclude(r => r.Empleado!).ThenInclude(e => e!.Usuario)
                .Where(f => f.Reserva != null && f.Reserva.Empleado != null)
                .ToListAsync();

            vm.EmpleadosTop = facturasConDetalle
                .GroupBy(f => f.Reserva!.EmpleadoId)
                .Select(g => new EmpleadoTop
                {
                    Nombre = g.First().Reserva!.Empleado!.Usuario?.Nombre ?? "Sin nombre",
                    ServiciosRealizados = g.Count(),
                    TotalGenerado = g.Sum(f => f.Total),
                    ComisionGanada = g.Sum(f => f.Total * (g.First().Reserva!.Empleado!.ComisionPorcentaje / 100))
                })
                .OrderByDescending(e => e.TotalGenerado)
                .Take(5)
                .ToList();

            return View(vm);
        }

        // GET: Reportes/Ventas?fechaInicio=...&fechaFin=...
        public async Task<IActionResult> Ventas(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            var inicio = DateTime.SpecifyKind(fechaInicio?.Date ?? DateTime.UtcNow.AddDays(-30).Date, DateTimeKind.Utc);
            var fin = DateTime.SpecifyKind((fechaFin?.Date ?? DateTime.UtcNow.Date).AddDays(1), DateTimeKind.Utc);

            var facturas = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.MetodoPago)
                .Include(f => f.Detalles!).ThenInclude(d => d.Servicio)
                .Where(f => f.FechaEmision >= inicio && f.FechaEmision < fin)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin.AddDays(-1);
            ViewBag.Total = facturas.Sum(f => f.Total);

            return View(facturas);
        }
    }
}
