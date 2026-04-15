using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Secorvi.Models
{
    public class Empleado : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Matricula { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string TurnoTipo { get; set; }
        public string PuntoServicio { get; set; }

        public bool EsAdmin { get; set; }
        public bool EsSuperAdmin { get; set; }
        public string Password { get; set; }
        public string UltimoReporteHora { get; set; } = "SIN REPORTE";
        public string UltimoReporteStatus { get; set; } = "PENDIENTE";
        public string UltimoReporteUbicacion { get; set; } = "Esperando señal...";
        public string ColorStatus { get; set; } = "#4B5563"; 

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}