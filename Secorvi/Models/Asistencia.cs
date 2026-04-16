namespace Secorvi.Models
{
    public class Asistencia
    {
        public int Id { get; set; }
        public int IdEmpleado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public TimeSpan Hora { get; set; }
        public string Estatus { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string LinkMapa => $"https://www.google.com/maps?q={Latitud},{Longitud}";
    }
}
