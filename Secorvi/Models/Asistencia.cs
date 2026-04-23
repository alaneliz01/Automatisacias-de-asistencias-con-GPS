using System;

namespace Secorvi.Models
{
    public class Asistencia
    {
        public int IdRegistro { get; set; }

        public int IdEmpleado { get; set; }

        // Coincide con fecha_inicio (DATE)
        public DateTime FechaInicio { get; set; }

        // Coincide con hora_inicio (TIME)
        public TimeSpan HoraInicio { get; set; }

        public DateTime? FechaFin { get; set; }
        public TimeSpan? HoraFin { get; set; }

        public string MetodoRegistro { get; set; } = "GPS";
       

        public double Latitud { get; set; }
        public double Longitud { get; set; }

        // Coincide con link_mapa de tu SQL
        public string? LinkMapa { get; set; }
    }
}