using Secorvi.Models;

namespace Secorvi
{
    public static class SesionActual
    {
        // El objeto completo del empleado que se logueó
        public static Empleado Usuario { get; set; }

        // CORRECCIÓN: id_rol en minúsculas para coincidir con el Modelo Empleado
        public static bool EsSuperAdmin => Usuario != null && Usuario.id_rol == 1;

        // CORRECCIÓN: id_rol en minúsculas
        public static bool EsAdminGestion => Usuario != null && (Usuario.id_rol == 1 || Usuario.id_rol == 2);

        public static void Logout()
        {
            Usuario = null;
        }
    }
}