namespace Secorvi.Models
{
    public class AsignacionDetalle
    {
        public int id_asignaciones { get; set; }
        public int id_empleado { get; set; }
        public string empleado { get; set; }
        public string ubicacion { get; set; }
        public string turno { get; set; }
        public string estatus { get; set; }
        public DateTime fecha { get; set; }
    }
}