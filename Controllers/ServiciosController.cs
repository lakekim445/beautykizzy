using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;
namespace BeautyKizzy.Controllers;
using BeautyKizzy.Services;

public class ProductoUsadoInput
{
    public string NombreProducto { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public decimal CantidadUsada { get; set; }
}

public class ServicioFormViewModel
{
    public Servicio Servicio { get; set; } = new();

    public IFormFile? Imagen { get; set; }

    public List<ProductoUsadoInput> ProductosUsados { get; set; } = new();

    public List<Categoria> Categorias { get; set; } = new();
}
public class ServiciosController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly SupabaseStorageService _storageService;

    public ServiciosController(
        ApplicationDbContext context,
        SupabaseStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }
    public async Task<IActionResult> Index()
    {
        var servicios = await _context.Servicios
            .Include(s => s.Categoria)
            .Where(s => s.Activo)
            .ToListAsync();
        return View(servicios);
    }

    public async Task<IActionResult> Create()
    {
        if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

        var vm = new ServicioFormViewModel
        {
            Categorias = await _context.Categorias.ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServicioFormViewModel vm)
    {
        if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");

        ModelState.Remove("Servicio.Categoria");
        ModelState.Remove("ProductosUsados");

        if (!ModelState.IsValid)
        {
            vm.Categorias = await _context.Categorias.ToListAsync();
            return View(vm);
        }

        var servicio = vm.Servicio;
        servicio.Activo = true;
        _context.Servicios.Add(servicio);
        await _context.SaveChangesAsync(); // necesitamos el Id del servicio antes de crear la receta

        foreach (var item in vm.ProductosUsados)
        {
            if (string.IsNullOrWhiteSpace(item.NombreProducto) || item.CantidadUsada <= 0)
                continue;

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Nombre.ToLower() == item.NombreProducto.Trim().ToLower());

            // El producto no existe todavía en inventario: se crea automáticamente.
            // Queda con Stock = 0; el stock real se carga después con una "compra".
            if (producto == null)
            {
                producto = new Producto
                {
                    Nombre = item.NombreProducto.Trim(),
                    UnidadMedida = string.IsNullOrWhiteSpace(item.Unidad) ? "unidad" : item.Unidad.Trim(),
                    StockActual = 0,
                    StockMinimo = 0
                };
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
            }

            _context.ServicioProductos.Add(new ServicioProducto
            {
                ServicioId = servicio.Id,
                ProductoId = producto.Id,
                Cantidad = item.CantidadUsada
            });
        }

        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Servicio creado exitosamente";
        return RedirectToAction(nameof(Index));
    }

    // GET: Servicios/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
        if (id == null) return NotFound();

        var servicio = await _context.Servicios
            .Include(s => s.ServicioProductos!)
            .ThenInclude(sp => sp.Producto)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (servicio == null) return NotFound();

        var vm = new ServicioFormViewModel
        {
            Servicio = servicio,
            Categorias = await _context.Categorias.ToListAsync(),
            ProductosUsados = servicio.ServicioProductos?.Select(sp => new ProductoUsadoInput
            {
                NombreProducto = sp.Producto?.Nombre ?? "",
                Unidad = sp.Producto?.UnidadMedida ?? "",
                CantidadUsada = sp.Cantidad
            }).ToList() ?? new List<ProductoUsadoInput>()
        };

        return View(vm);
    }

    // POST: Servicios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServicioFormViewModel vm)
    {
        if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
        if (id != vm.Servicio.Id) return NotFound();

        ModelState.Remove("Servicio.Categoria");
        ModelState.Remove("ProductosUsados");

        if (!ModelState.IsValid)
        {
            vm.Categorias = await _context.Categorias.ToListAsync();
            return View(vm);
        }

        var existente = await _context.Servicios
            .Include(s => s.ServicioProductos)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (existente == null) return NotFound();

        existente.Nombre = vm.Servicio.Nombre;
        existente.CategoriaId = vm.Servicio.CategoriaId;
        existente.DuracionMinutos = vm.Servicio.DuracionMinutos;
        existente.PrecioBase = vm.Servicio.PrecioBase;
        existente.Descripcion = vm.Servicio.Descripcion;

        // Reemplazamos la receta completa: más simple y predecible que intentar
        // hacer un diff fino de qué cambió.
        if (existente.ServicioProductos != null)
        {
            _context.ServicioProductos.RemoveRange(existente.ServicioProductos);
        }

        foreach (var item in vm.ProductosUsados)
        {
            if (string.IsNullOrWhiteSpace(item.NombreProducto) || item.CantidadUsada <= 0)
                continue;

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Nombre.ToLower() == item.NombreProducto.Trim().ToLower());

            if (producto == null)
            {
                producto = new Producto
                {
                    Nombre = item.NombreProducto.Trim(),
                    UnidadMedida = string.IsNullOrWhiteSpace(item.Unidad) ? "unidad" : item.Unidad.Trim(),
                    StockActual = 0,
                    StockMinimo = 0
                };
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
            }

            _context.ServicioProductos.Add(new ServicioProducto
            {
                ServicioId = existente.Id,
                ProductoId = producto.Id,
                Cantidad = item.CantidadUsada
            });
        }

        await _context.SaveChangesAsync();
        TempData["Mensaje"] = "Servicio actualizado";
        return RedirectToAction(nameof(Index));
    }

    // GET: Servicios/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (!EsGerenteOAdmin()) return RedirectToAction("Login", "Account");
        if (id == null) return NotFound();

        var servicio = await _context.Servicios
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (servicio == null) return NotFound();

        return View(servicio);
    }

    // POST: Servicios/Delete/5
    // No se borra físicamente: se desactiva, para no perder el historial
    // de citas/facturas que ya lo referencian.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var servicio = await _context.Servicios.FindAsync(id);
        if (servicio != null)
        {
            servicio.Activo = false;
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Servicio desactivado";
        }
        return RedirectToAction(nameof(Index));
    }

}
