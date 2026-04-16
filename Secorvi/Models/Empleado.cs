namespace Secorvi.Models
{
    public class Empleado
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; } 
        public string Matricula { get; set; } 
        public string Telefono { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public bool EsAdmin { get; set; }
        public bool Activo { get; set; }
        public string Rol => EsAdmin ? "ADMIN" : "AGENTE";
        public string RoleColor => EsAdmin ? "#FFB300" : "#4B5563";
    }
}