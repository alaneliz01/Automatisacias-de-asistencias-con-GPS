using System;

namespace Secorvi.Models
{
    public class Asignacion
    {
        // Coincide con id_asignacion (PRIMARY KEY)
        public int id_asignacion { get; set; }

        public int id_empleado { get; set; }

        public int id_ubicacion { get; set; }

        // Nueva columna en la DB para el nombre del turno (Matutino, Nocturno, etc.)
        public string descripcion_del_turno { get; set; }

        public DateTime fecha { get; set; }

        // Coincide con TIME en MySQL
        public TimeSpan hora_inicio { get; set; }

        public TimeSpan hora_fin { get; set; }

        // Estatus: PROGRAMADO, PRESENTE, FALTA, etc.
        public string estatus { get; set; }
    }
}