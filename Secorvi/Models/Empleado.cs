namespace Secorvi.Models
{
    public class Empleado
    {
        public int id_empleado { get; set; }
        public string nombre_completo { get; set; }
        public string matricula { get; set; }
        public string telefono { get; set; }
        public string usuario { get; set; } 
        public string contrasena { get; set; }
        public int id_rol { get; set; }
        // 1. Para la Asistencia (n8n actualizará esto en la BD)
        public string estatus_asistencia { get; set; } // "PRESENTE", "FALTA", "PENDIENTE"
        // 2. Para las horas (Aquí guardaremos el texto del turno)
        public string info_turno { get; set; } // Ejemplo: "07:00 AM - 03:00 PM"
        public string estatus { get; set; }
    }
}