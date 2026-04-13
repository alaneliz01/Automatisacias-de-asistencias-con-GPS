using System;

// Esta clase representa un registro de asistencia de un empleado, incluyendo la fecha y hora del registro, la ubicación (latitud y longitud)
namespace Secorvi.Models
{
    public class Asistencia
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public DateTime FechaHoraRegistro { get; set; }
        public double LatitudRecibida { get; set; }
        public double LongitudRecibida { get; set; }
        public bool EsValida { get; set; } 
        public string Observaciones { get; set; } // Ejemplo: "Fuera de rango por 500m"
    }
}