using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class HerramientasController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public HerramientasController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            var herramientas = await _context.Herramientas.ToListAsync();

            // La disponibilidad se recalcula aquí, no se guarda fija:
            // CantidadDisponible = CantidadTotal - cuántas están comprometidas
            // en reservas que están EnCurso en este momento.
            var ahora = DateTime.UtcNow;
            foreach (var herramienta in herramientas)
            {
                var enUso = await (
                    from sh in _context.ServicioHerramientas
                    join r in _context.Reservas on sh.ServicioId equals r.ServicioId
                    where sh.HerramientaId == herramienta.Id
                          && r.Estado == EstadoReserva.EnCurso
                    select sh.CantidadNecesaria
                ).SumAsync();

                herramienta.CantidadDisponible = herramienta.CantidadTotal - enUso;
            }

            return View(herramientas);
        }

        public IActionResult Create()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Herramienta herramienta)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(herramienta);

            herramienta.CantidadDisponible = herramienta.CantidadTotal;
            _context.Herramientas.Add(herramienta);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Herramienta agregada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var herramienta = await _context.Herramientas.FindAsync(id);
            if (herramienta == null) return NotFound();
            return View(herramienta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Herramienta herramienta)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id != herramienta.Id) return NotFound();
            if (!ModelState.IsValid) return View(herramienta);

            _context.Update(herramienta);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Herramienta actualizada";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var herramienta = await _context.Herramientas.FindAsync(id);
            if (herramienta == null) return NotFound();
            return View(herramienta);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var herramienta = await _context.Herramientas.FindAsync(id);
            if (herramienta != null)
            {
                herramienta.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Herramienta desactivada";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
