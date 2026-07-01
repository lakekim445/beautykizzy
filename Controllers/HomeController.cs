using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class HomeViewModel
    {
        public int TotalServicios { get; set; }
        public int TotalEmpleados { get; set; }
        public int TotalCategorias { get; set; }
        public List<Servicio> ServiciosDestacados { get; set; } = new();
    }

    public class HomeController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Home (página pública, no requiere login)
        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel
            {
                TotalServicios = await _context.Servicios.CountAsync(s => s.Activo),
                TotalEmpleados = await _context.Empleados.CountAsync(e => e.Activo),
                TotalCategorias = await _context.Categorias.CountAsync(c => c.Activo),
                ServiciosDestacados = await _context.Servicios
                    .Include(s => s.Categoria)
                    .Where(s => s.Activo)
                    .Take(4)
                    .ToListAsync()
            };

            return View(vm);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
