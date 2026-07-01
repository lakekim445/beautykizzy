using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class PromocionFormViewModel
    {
        public Promocion Promocion { get; set; } = new();
        public List<int> ServiciosSeleccionados { get; set; } = new();
        public List<Servicio> ServiciosDisponibles { get; set; } = new();
    }

    public class PromocionesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public PromocionesController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Promociones (visible para todos, igual que el catálogo de servicios)
        public async Task<IActionResult> Index()
        {
            var promociones = await _context.Promociones
                .Include(p => p.PromocionServicios!)
                .ThenInclude(ps => ps.Servicio)
                .Where(p => p.Activa && p.FechaFin >= DateTime.UtcNow)
                .ToListAsync();
            return View(promociones);
        }

        public async Task<IActionResult> Create()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            var vm = new PromocionFormViewModel
            {
                ServiciosDisponibles = await _context.Servicios.Where(s => s.Activo).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromocionFormViewModel vm)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            ModelState.Remove("Promocion.PromocionServicios");

            if (vm.ServiciosSeleccionados.Count < 2)
            {
                ModelState.AddModelError("ServiciosSeleccionados", "Selecciona al menos 2 servicios para el combo");
            }

            if (!ModelState.IsValid)
            {
                vm.ServiciosDisponibles = await _context.Servicios.Where(s => s.Activo).ToListAsync();
                return View(vm);
            }

            var promocion = vm.Promocion;
            promocion.Activa = true;
            _context.Promociones.Add(promocion);
            await _context.SaveChangesAsync();

            foreach (var servicioId in vm.ServiciosSeleccionados)
            {
                _context.PromocionServicios.Add(new PromocionServicio
                {
                    PromocionId = promocion.Id,
                    ServicioId = servicioId
                });
            }
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Promoción creada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var promocion = await _context.Promociones
                .Include(p => p.PromocionServicios)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (promocion == null) return NotFound();

            var vm = new PromocionFormViewModel
            {
                Promocion = promocion,
                ServiciosSeleccionados = promocion.PromocionServicios?.Select(ps => ps.ServicioId).ToList() ?? new(),
                ServiciosDisponibles = await _context.Servicios.Where(s => s.Activo).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromocionFormViewModel vm)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id != vm.Promocion.Id) return NotFound();

            ModelState.Remove("Promocion.PromocionServicios");

            if (vm.ServiciosSeleccionados.Count < 2)
            {
                ModelState.AddModelError("ServiciosSeleccionados", "Selecciona al menos 2 servicios para el combo");
            }

            if (!ModelState.IsValid)
            {
                vm.ServiciosDisponibles = await _context.Servicios.Where(s => s.Activo).ToListAsync();
                return View(vm);
            }

            var existente = await _context.Promociones
                .Include(p => p.PromocionServicios)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (existente == null) return NotFound();

            existente.Nombre = vm.Promocion.Nombre;
            existente.PrecioPromocion = vm.Promocion.PrecioPromocion;
            existente.FechaInicio = vm.Promocion.FechaInicio;
            existente.FechaFin = vm.Promocion.FechaFin;

            if (existente.PromocionServicios != null)
            {
                _context.PromocionServicios.RemoveRange(existente.PromocionServicios);
            }
            foreach (var servicioId in vm.ServiciosSeleccionados)
            {
                _context.PromocionServicios.Add(new PromocionServicio
                {
                    PromocionId = existente.Id,
                    ServicioId = servicioId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Promoción actualizada";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var promocion = await _context.Promociones
                .Include(p => p.PromocionServicios!)
                .ThenInclude(ps => ps.Servicio)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (promocion == null) return NotFound();
            return View(promocion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promocion = await _context.Promociones.FindAsync(id);
            if (promocion != null)
            {
                promocion.Activa = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Promoción desactivada";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
