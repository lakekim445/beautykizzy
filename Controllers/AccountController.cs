using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos");
                return View(model);
            }

            // Guardamos en sesión los datos clave: quién es y qué rol tiene
            HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
            HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);

            // Cada rol aterriza en su propia pantalla principal
            return RedirigirSegunRol();
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        // El registro público siempre crea un Cliente.
        // Los Empleados y el Admin se crean desde el panel de administración (no aquí).
        [HttpPost]
        public async Task<IActionResult> Register(Usuario usuario)
        {
            ModelState.Remove(nameof(Usuario.Rol));
            ModelState.Remove(nameof(Usuario.Empleado));

            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con este correo");
                return View(usuario);
            }

            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            usuario.Rol = Roles.Cliente;
            usuario.FechaRegistro = DateTime.UtcNow;

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Cuenta creada exitosamente. Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        // GET/POST: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
