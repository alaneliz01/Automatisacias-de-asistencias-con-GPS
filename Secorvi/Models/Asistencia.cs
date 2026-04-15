using System;

namespace Secorvi.Models
{
    public class Asistencia
    {
        public int Id { get; set; }
        public int IdEmpleado { get; set; }
        public DateTime FechaHora { get; set; }
        public double LatitudRecibida { get; set; }
        public double LongitudRecibida { get; set; }
        public bool DentroDeRango { get; set; }
        public string Incidencias { get; set; }
        public string LinkMapa => $"https://www.google.com/maps?q={LatitudRecibida},{LongitudRecibida}";
    }
}