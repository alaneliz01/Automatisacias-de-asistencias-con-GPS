namespace Secorvi.Models
{
    public class Empleado
    {
        public int id_empleado { get; set; }
        public string nombre_completo { get; set; }
        public string telefono { get; set; }
        public int id_rol { get; set; }
        public string estatus { get; set; }
        public string usuario { get; set; }
        public string contrasena { get; set; }
        public string matricula { get; set; }
        public string estatus_asistencia { get; set; }
        public string info_turno { get; set; }
        public string info_turno_entrada { get; set; }
        public string info_turno_salida { get; set; }
        public string info_prog_entrada { get; set; }
        public string info_prog_salida { get; set; }
    }
}