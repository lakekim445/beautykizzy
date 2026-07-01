using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    // ViewModel: datos de Usuario + Empleado combinados en un solo formulario
    public class EmpleadoFormViewModel
    {
        public int Id { get; set; } // Id del Empleado (0 si es nuevo)
        public int UsuarioId { get; set; } // Id del Usuario asociado (0 si es nuevo)

        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; } // solo obligatorio al crear
        public string? Telefono { get; set; }

        public int CategoriaId { get; set; }
        public string? Especialidad { get; set; }
        public decimal ComisionPorcentaje { get; set; } = 20;
        public bool Activo { get; set; } = true;

        public List<Categoria> Categorias { get; set; } = new();
    }

    public class EmpleadosController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public EmpleadosController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Empleados
        public async Task<IActionResult> Index()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            var empleados = await _context.Empleados
                .Include(e => e.Usuario)
                .Include(e => e.Categoria)
                .ToListAsync();
            return View(empleados);
        }

        // GET: Empleados/Create
        public async Task<IActionResult> Create()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            var vm = new EmpleadoFormViewModel
            {
                Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync()
            };
            return View(vm);
        }

        // POST: Empleados/Create
        // Crea el Usuario (con Rol=Empleado) y el registro de Empleado en un solo paso.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpleadoFormViewModel vm)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError("Password", "La contraseña es requerida para un empleado nuevo");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con este correo");
            }

            if (!ModelState.IsValid)
            {
                vm.Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync();
                return View(vm);
            }

            var usuario = new Usuario
            {
                Nombre = vm.Nombre,
                Email = vm.Email,
                Password = vm.Password!,
                Telefono = vm.Telefono,
                Rol = Roles.Empleado,
                FechaRegistro = DateTime.UtcNow
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(); // necesitamos el Id del usuario antes de crear el Empleado

            var empleado = new Empleado
            {
                UsuarioId = usuario.Id,
                CategoriaId = vm.CategoriaId,
                Especialidad = vm.Especialidad,
                ComisionPorcentaje = vm.ComisionPorcentaje,
                Activo = true,
                FechaContratacion = DateTime.UtcNow
            };
            _context.Empleados.Add(empleado);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Empleado registrado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // GET: Empleados/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null || empleado.Usuario == null) return NotFound();

            var vm = new EmpleadoFormViewModel
            {
                Id = empleado.Id,
                UsuarioId = empleado.UsuarioId,
                Nombre = empleado.Usuario.Nombre,
                Email = empleado.Usuario.Email,
                Telefono = empleado.Usuario.Telefono,
                CategoriaId = empleado.CategoriaId,
                Especialidad = empleado.Especialidad,
                ComisionPorcentaje = empleado.ComisionPorcentaje,
                Activo = empleado.Activo,
                Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync()
            };

            return View(vm);
        }

        // POST: Empleados/Edit/5
        // La contraseña NO se edita aquí (no actualizamos Password si viene vacío),
        // para no obligar a re-escribirla cada vez que se edita otro dato.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmpleadoFormViewModel vm)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync();
                return View(vm);
            }

            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null || empleado.Usuario == null) return NotFound();

            empleado.Usuario.Nombre = vm.Nombre;
            empleado.Usuario.Email = vm.Email;
            empleado.Usuario.Telefono = vm.Telefono;
            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                empleado.Usuario.Password = vm.Password;
            }

            empleado.CategoriaId = vm.CategoriaId;
            empleado.Especialidad = vm.Especialidad;
            empleado.ComisionPorcentaje = vm.ComisionPorcentaje;
            empleado.Activo = vm.Activo;

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Empleado actualizado";
            return RedirectToAction(nameof(Index));
        }

        // GET: Empleados/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .Include(e => e.Categoria)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null) return NotFound();

            return View(empleado);
        }

        // POST: Empleados/Delete/5
        // No se borra físicamente: se desactiva, porque puede tener reservas/facturas
        // históricas asociadas que no deben perderse.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado != null)
            {
                empleado.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Empleado desactivado";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
