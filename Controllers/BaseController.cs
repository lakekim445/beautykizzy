using Microsoft.AspNetCore.Mvc;
using BeautyKizzy.Models;

namespace BeautyKizzy.Controllers
{
    public abstract class BaseController : Controller
    {
        protected bool UsuarioLogueado() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId"));

        protected int GetUsuarioId() => int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");

        protected string? GetRol() => HttpContext.Session.GetString("UsuarioRol");

        protected bool EsAdmin() => GetRol() == Roles.Admin;

        protected bool EsGerente() => GetRol() == Roles.Gerente;

        protected bool EsGerenteOAdmin() => EsGerente() || EsAdmin();

        protected bool EsEmpleado() => GetRol() == Roles.Empleado;

        // "Personal" = cualquiera que trabaje en el salón (no Cliente)
        protected bool EsPersonal() => EsEmpleado() || EsGerenteOAdmin();

        // Redirige a la pantalla principal de cada rol, usado tras login
        // y como destino por defecto cuando alguien intenta entrar a algo sin permiso.
        protected IActionResult RedirigirSegunRol()
        {
            return GetRol() switch
            {
                Roles.Admin => RedirectToAction("Dashboard", "Reportes"),
                Roles.Gerente => RedirectToAction("Index", "Reservas"),
                Roles.Empleado => RedirectToAction("MiAgenda", "Reservas"),
                Roles.Cliente => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Login", "Account")
            };
        }
    }
}
