using System;

namespace Secorvi.Models
{
    public class Turno
    {
        public int id_turno { get; set; } // En minúsculas
        public int? id_lugar { get; set; }
        public int? id_empleado { get; set; }
        public string nombre { get; set; }
        public TimeSpan hora_inicio { get; set; }
        public TimeSpan hora_fin { get; set; }
    }
}