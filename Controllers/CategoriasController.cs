using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class CategoriasController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Categorias
        public async Task<IActionResult> Index()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            var categorias = await _context.Categorias.ToListAsync();
            return View(categorias);
        }

        // GET: Categorias/Create
        public IActionResult Create()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        // POST: Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            if (await _context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría con ese nombre");
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Categoría creada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // POST: Categorias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id != categoria.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            _context.Update(categoria);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Categoría actualizada";
            return RedirectToAction(nameof(Index));
        }

        // GET: Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // POST: Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria != null)
            {
                _context.Categorias.Remove(categoria);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Categoría eliminada";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
