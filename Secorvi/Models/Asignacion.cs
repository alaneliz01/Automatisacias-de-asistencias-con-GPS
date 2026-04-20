namespace Secorvi.Models
{
    public class Asignacion
    {
        public int id_asignaciones { get; set; }
        public int id_empleado { get; set; }
        public int id_ubicacion { get; set; }
        public int id_turno { get; set; } // Propiedad faltante
        public DateTime fecha { get; set; }
        public string estatus { get; set; }
    }
}

//cambios echos :D