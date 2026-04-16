namespace Secorvi.Models
{
    public class Asignacion
    {
        public int Id { get; set; }
        public int IdEmpleado { get; set; }
        public int? IdUbicacion { get; set; } 
        public int IdTurno { get; set; }
        public DateTime Fecha { get; set; }
        public string Estatus { get; set; }
    }
}