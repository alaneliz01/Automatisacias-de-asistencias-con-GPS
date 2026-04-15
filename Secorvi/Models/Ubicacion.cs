namespace Secorvi.Models
{
    public class Ubicacion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string IdCliente { get; set; } // Relación con cliente
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public double RadioPermitido { get; set; }
        public bool Activo { get; set; }
    }
}