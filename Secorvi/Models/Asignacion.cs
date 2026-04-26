using System;

namespace Secorvi.Models
{
    public class Asignacion
    {
        public int id_asignacion { get; set; }
        public int id_empleado { get; set; }
        public int id_ubicacion { get; set; }
        public string descripcion_del_turno { get; set; }
        public DateTime fecha { get; set; }
        public TimeSpan hora_inicio { get; set; }
        public TimeSpan hora_fin { get; set; }
        public string estatus { get; set; }
    }
}