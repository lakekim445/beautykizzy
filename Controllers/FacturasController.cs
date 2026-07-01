using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    // ViewModel para el formulario de cierre de una reserva
    public class CerrarReservaViewModel
    {
        public int ReservaId { get; set; }
        public string? ServicioNombre { get; set; }
        public string? ClienteNombre { get; set; }
        public decimal PrecioBase { get; set; }

        public decimal CargosExtra { get; set; } = 0;
        public string? DescripcionExtra { get; set; }
        public decimal Descuento { get; set; } = 0;
        public int MetodoPagoId { get; set; }

        public List<MetodoPago> MetodosPago { get; set; } = new();
    }

    public class FacturasController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public FacturasController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Facturas/Cerrar/5  (id = ReservaId)
        // Pantalla donde el empleado/admin registra el cobro al terminar el servicio.
        public async Task<IActionResult> Cerrar(int id)
        {
            if (!EsEmpleado() && !EsGerenteOAdmin())
                return RedirectToAction("Login", "Account");

            var reserva = await _context.Reservas
                .Include(r => r.Servicio)
                .Include(r => r.Cliente)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null || reserva.Servicio == null) return NotFound();

            if (reserva.Estado == EstadoReserva.Completada)
            {
                TempData["Mensaje"] = "Esta reserva ya fue facturada";
                return RedirectToAction("MiAgenda", "Reservas");
            }

            var vm = new CerrarReservaViewModel
            {
                ReservaId = reserva.Id,
                ServicioNombre = reserva.Servicio.Nombre,
                ClienteNombre = reserva.Cliente?.Nombre,
                PrecioBase = reserva.Servicio.PrecioBase,
                MetodosPago = await _context.MetodosPago.Where(m => m.Activo).ToListAsync()
            };

            return View(vm);
        }

        // POST: Facturas/Cerrar
        // Al confirmar el cobro: 1) marca la Reserva como Completada,
        // 2) descuenta del inventario según la "receta" del servicio (sin importar
        //    los cargos extra de precio, como acordamos), 3) crea la Factura.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(CerrarReservaViewModel vm)
        {
            if (!EsEmpleado() && !EsGerenteOAdmin())
                return RedirectToAction("Login", "Account");

            var reserva = await _context.Reservas
                .Include(r => r.Servicio!).ThenInclude(s => s.ServicioProductos)
                .FirstOrDefaultAsync(r => r.Id == vm.ReservaId);

            if (reserva == null || reserva.Servicio == null) return NotFound();

            if (vm.MetodoPagoId == 0)
            {
                ModelState.AddModelError("MetodoPagoId", "Selecciona un método de pago");
            }

            if (!ModelState.IsValid)
            {
                vm.MetodosPago = await _context.MetodosPago.Where(m => m.Activo).ToListAsync();
                vm.ServicioNombre = reserva.Servicio.Nombre;
                vm.PrecioBase = reserva.Servicio.PrecioBase;
                return View(vm);
            }

            var subtotal = reserva.Servicio.PrecioBase + vm.CargosExtra;
            var total = subtotal - vm.Descuento;

            var factura = new Factura
            {
                ClienteId = reserva.ClienteId,
                ReservaId = reserva.Id,
                MetodoPagoId = vm.MetodoPagoId,
                NumeroFactura = $"FAC-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Subtotal = subtotal,
                Descuento = vm.Descuento,
                Iva = 0,
                Total = total,
                FechaEmision = DateTime.UtcNow
            };
            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();

            _context.FacturaDetalles.Add(new FacturaDetalle
            {
                FacturaId = factura.Id,
                ServicioId = reserva.ServicioId,
                PrecioUnitario = reserva.Servicio.PrecioBase,
                CargosExtra = vm.CargosExtra,
                Descuento = vm.Descuento,
                Subtotal = total,
                DescripcionExtra = vm.DescripcionExtra
            });

            // Descuento automático de inventario, según la receta del servicio.
            // Esto es independiente de los cargos extra de precio (regla acordada).
            if (reserva.Servicio.ServicioProductos != null)
            {
                foreach (var receta in reserva.Servicio.ServicioProductos)
                {
                    var producto = await _context.Productos.FindAsync(receta.ProductoId);
                    if (producto == null) continue;

                    producto.StockActual -= receta.Cantidad;
                    if (producto.StockActual < 0) producto.StockActual = 0;

                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = TipoMovimiento.Salida,
                        Cantidad = receta.Cantidad,
                        ReferenciaId = reserva.Id,
                        Observacion = $"Consumido en servicio: {reserva.Servicio.Nombre}"
                    });

                    // Alerta de stock bajo, si corresponde
                    if (producto.StockActual <= producto.StockMinimo)
                    {
                        var admins = await _context.Usuarios.Where(u => u.Rol == Roles.Admin).ToListAsync();
                        foreach (var admin in admins)
                        {
                            _context.Notificaciones.Add(new Notificacion
                            {
                                UsuarioId = admin.Id,
                                Titulo = "Stock bajo",
                                Mensaje = $"El producto '{producto.Nombre}' está por agotarse ({producto.StockActual} {producto.UnidadMedida} restantes)",
                                Tipo = TipoNotificacion.StockBajo
                            });
                        }
                    }
                }
            }

            reserva.Estado = EstadoReserva.Completada;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Servicio facturado exitosamente";
            return RedirectToAction(nameof(Detalle), new { id = factura.Id });
        }

        // GET: Facturas/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.MetodoPago)
                .Include(f => f.Reserva!).ThenInclude(r => r.Empleado!).ThenInclude(e => e!.Usuario)
                .Include(f => f.Detalles!).ThenInclude(d => d.Servicio)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();

            // Un cliente solo puede ver sus propias facturas; empleado/admin ven todas.
            if (GetRol() == Roles.Cliente && factura.ClienteId != GetUsuarioId())
                return Forbid();

            return View(factura);
        }

        // GET: Facturas (listado, solo Admin)
        public async Task<IActionResult> Index()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            var facturas = await _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.MetodoPago)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            return View(facturas);
        }
    }
}
