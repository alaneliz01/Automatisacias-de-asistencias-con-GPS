namespace Secorvi.Models
{
    public class Ubicacion
    {
        public int id_ubicacion { get; set; }
        public string nombre_lugar { get; set; }
        public decimal latitud { get; set; }
        public decimal longitud { get; set; }
        public int radio_permitido { get; set; }
    }
}