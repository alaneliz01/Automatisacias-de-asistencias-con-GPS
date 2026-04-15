namespace Secorvi
{
    // Esta clase es como una caja global que guarda al jefe que inició sesión
    public static class SesionActual
    {
        // Aquí guardaremos el objeto Empleado del admin que hizo Login
        public static Models.Empleado Usuario { get; set; }
    }
}