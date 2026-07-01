using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class RegistrarCompraViewModel
    {
        public int ProductoId { get; set; }
        public string? ProductoNombre { get; set; }
        public decimal Cantidad { get; set; }
        public string? Observacion { get; set; }
    }

    public class ProductosController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Productos (inventario completo, con alerta visual de stock bajo)
        public async Task<IActionResult> Index()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            var productos = await _context.Productos
                .Include(p => p.Proveedor)
                .Where(p => p.Activo)
                .ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Create()
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Activo).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            if (await _context.Productos.AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.Trim().ToLower()))
            {
                ModelState.AddModelError("Nombre", "Ya existe un producto con ese nombre");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Activo).ToListAsync();
                return View(producto);
            }

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // Si se creó con stock inicial > 0, lo dejamos también como un movimiento
            // de Entrada, para que el historial de inventario sea completo desde el día 1.
            if (producto.StockActual > 0)
            {
                _context.MovimientosInventario.Add(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = TipoMovimiento.Entrada,
                    Cantidad = producto.StockActual,
                    Observacion = "Stock inicial al crear el producto"
                });
                await _context.SaveChangesAsync();
            }

            TempData["Mensaje"] = "Producto creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Activo).ToListAsync();
            return View(producto);
        }

        // Edit NO modifica el Stock directamente (eso solo pasa vía RegistrarCompra
        // o automáticamente al completar una reserva) — aquí solo se editan datos
        // descriptivos: nombre, unidad, proveedor, costo, stock mínimo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id != producto.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Activo).ToListAsync();
                return View(producto);
            }

            var existente = await _context.Productos.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nombre = producto.Nombre;
            existente.Descripcion = producto.Descripcion;
            existente.UnidadMedida = producto.UnidadMedida;
            existente.StockMinimo = producto.StockMinimo;
            existente.CostoUnitario = producto.CostoUnitario;
            existente.ProveedorId = producto.ProveedorId;

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Producto actualizado";
            return RedirectToAction(nameof(Index));
        }

        // GET: Productos/RegistrarCompra/5
        // Pantalla para "recargar" stock de un producto existente.
        public async Task<IActionResult> RegistrarCompra(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            var vm = new RegistrarCompraViewModel
            {
                ProductoId = producto.Id,
                ProductoNombre = producto.Nombre
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCompra(RegistrarCompraViewModel vm)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

            if (vm.Cantidad <= 0)
            {
                ModelState.AddModelError("Cantidad", "La cantidad debe ser mayor a 0");
            }

            var producto = await _context.Productos.FindAsync(vm.ProductoId);
            if (producto == null) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.ProductoNombre = producto.Nombre;
                return View(vm);
            }

            producto.StockActual += vm.Cantidad;

            _context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                TipoMovimiento = TipoMovimiento.Entrada,
                Cantidad = vm.Cantidad,
                Observacion = vm.Observacion ?? "Compra de stock"
            });

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se agregaron {vm.Cantidad} {producto.UnidadMedida} a {producto.Nombre}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                producto.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Producto desactivado";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
