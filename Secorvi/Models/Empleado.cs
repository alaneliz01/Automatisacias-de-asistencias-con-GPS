namespace Secorvi.Models
{
    public class Empleado
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Matricula { get; set; }
        public string Telefono { get; set; }
        public string Usuario { get; set; }      // Coincide con SQL
        public string Contrasena { get; set; }   // Coincide con SQL
        public string Rol { get; set; }         // 'ADMIN' o 'AGENTE'
        public bool Activo { get; set; }
        public bool CumplioAsistenciaHoy { get; set; }
        public bool EsAdmin => Rol == "ADMIN";
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}