using Microsoft.EntityFrameworkCore;
using BeautyKizzy.Models;

namespace BeautyKizzy.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<ServicioProducto> ServicioProductos { get; set; }
        public DbSet<Herramienta> Herramientas { get; set; }
        public DbSet<ServicioHerramienta> ServicioHerramientas { get; set; }
        public DbSet<Promocion> Promociones { get; set; }
        public DbSet<PromocionServicio> PromocionServicios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<MetodoPago> MetodosPago { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<FacturaDetalle> FacturaDetalles { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
    }
}
