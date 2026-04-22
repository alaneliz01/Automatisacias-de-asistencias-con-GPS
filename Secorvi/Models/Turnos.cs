using System;

namespace Secorvi.Models
{
    public class Turno
    {
        public int id_turno { get; set; }
        public int? id_lugar { get; set; }    // Agregalo de nuevo
        public int? id_empleado { get; set; } // Agregalo de nuevo
        public string nombre { get; set; }
        public TimeSpan hora_inicio { get; set; }
        public TimeSpan hora_fin { get; set; }
    }
}