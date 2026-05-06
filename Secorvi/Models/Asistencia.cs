using System;

namespace Secorvi.Models
{
    public class Asistencia
    {
        public int id_registro { get; set; }
        public int id_empleado { get; set; }
        public int id_asignacion { get; set; }
        public int id_ubicacion { get; set; }

        public DateTime fecha_inicio { get; set; }
        public TimeSpan hora_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public TimeSpan? hora_fin { get; set; }
        public string estatus { get; set; }
        public decimal? latitud { get; set; }
        public decimal? longitud { get; set; }
        public string? link_mapa { get; set; }
        public decimal? latitud_salida { get; set; }
        public decimal? longitud_salida { get; set; }
        public string? link_mapa_salida { get; set; }
        public string estado { get; set; }
        public string MetodoRegistro { get; set; } = "GPS";
    }
}