namespace Secorvi.Models
{
    // ubicacion aca usaremos lo del n8n o eso creo porque nose como jala esa madre jeje
    public class Ubicacion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public double RadioCerca { get; set; }
    }
}