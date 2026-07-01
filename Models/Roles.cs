namespace BeautyKizzy.Models
{
    /// <summary>
    /// Roles disponibles en el sistema
    /// </summary>
    public static class Roles
    {
        /// <summary>Usuario normal que agenda citas</summary>
        public const string Cliente = "Cliente";

        /// <summary>Estilista que solo ve sus citas</summary>
        public const string Empleado = "Empleado";

        /// <summary>Encargado del salón (gestiona todo menos empleados y reportes)</summary>
        public const string Gerente = "Gerente";

        /// <summary>Dueño del negocio (tiene acceso total)</summary>
        public const string Admin = "Admin";

        /// <summary>Obtiene todos los roles como lista</summary>
        public static List<string> Lista => new() { Cliente, Empleado, Gerente, Admin };

        /// <summary>Verifica si un rol es válido</summary>
        public static bool EsValido(string rol)
        {
            return Lista.Contains(rol);
        }

        /// <summary>Obtiene el nombre amigable del rol para mostrar</summary>
        public static string GetNombreAmigable(string rol)
        {
            return rol switch
            {
                Cliente => "Cliente",
                Empleado => "Empleado",
                Gerente => "Gerente",
                Admin => "Administrador",
                _ => rol
            };
        }

        /// <summary>
        /// Clase CSS para la badge de rol, usando la paleta del proyecto
        /// (rosa / sage / negro) en vez de los colores genéricos de Bootstrap.
        /// Estas clases se definen en el CSS compartido del layout.
        /// </summary>
        public static string GetColorBadge(string rol)
        {
            return rol switch
            {
                Cliente => "badge-rol-cliente",   // rosa claro
                Empleado => "badge-rol-empleado", // sage
                Gerente => "badge-rol-gerente",   // dorado
                Admin => "badge-rol-admin",       // negro
                _ => "badge-rol-cliente"
            };
        }
    }
}
