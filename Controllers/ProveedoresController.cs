using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class ProveedoresController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            return View(await _context.Proveedores.ToListAsync());
        }

        public IActionResult Create()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(proveedor);

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Proveedor creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proveedor proveedor)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id != proveedor.Id) return NotFound();
            if (!ModelState.IsValid) return View(proveedor);

            _context.Update(proveedor);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Proveedor actualizado";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        // Borrado suave: si el proveedor tiene productos asociados, desactivarlo
        // evita romper esa relación histórica.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                proveedor.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Proveedor desactivado";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
