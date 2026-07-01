using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Data;

namespace BeautyKizzy.Controllers
{
    public class NotificacionesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Notificaciones (las del usuario logueado, sin importar el rol)
        public async Task<IActionResult> Index()
        {
            if (!UsuarioLogueado()) return RedirectToAction("Login", "Account");

            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == GetUsuarioId())
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();

            return View(notificaciones);
        }

        // POST: Notificaciones/MarcarLeida/5
        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            if (!UsuarioLogueado()) return Json(new { success = false });

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == GetUsuarioId());

            if (notificacion == null) return Json(new { success = false });

            notificacion.Leida = true;
            notificacion.FechaLectura = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Notificaciones/ContadorNoLeidas (para el badge en el navbar)
        [HttpGet]
        public async Task<IActionResult> ContadorNoLeidas()
        {
            if (!UsuarioLogueado()) return Json(new { count = 0 });

            var count = await _context.Notificaciones
                .CountAsync(n => n.UsuarioId == GetUsuarioId() && !n.Leida);

            return Json(new { count });
        }
    }
}
