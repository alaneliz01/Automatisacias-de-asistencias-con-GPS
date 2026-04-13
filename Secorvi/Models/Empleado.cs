using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// Esta clase es para simular la base de datos de la tabla empleados, con propiedades como Id, Matricula, Nombre, Telefono, TurnoTipo, SueldoBase, TotalSueldo, PuntoServicio y los días de la semana.
namespace Secorvi.Models
{
    public class Empleado : INotifyPropertyChanged
    {
        private string _puntoServicio;
        private string _viernes = "0", _sabado = "0", _domingo = "0", _lunes = "0", _martes = "0", _miercoles = "0", _jueves = "0";
        // Propiedades del empleado
        public int Id { get; set; }
        public string Matricula { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string TurnoTipo { get; set; }
        public decimal SueldoBase { get; set; }
        public decimal TotalSueldo { get; set; }

        public string PuntoServicio
        {
            get => _puntoServicio;
            set { _puntoServicio = value; OnPropertyChanged(); }
        }
        // Días libres
        public bool ViernesLibre { get; set; }
        public bool SabadoLibre { get; set; }
        public bool DomingoLibre { get; set; }
        public bool LunesLibre { get; set; }
        public bool MartesLibre { get; set; }
        public bool MiercolesLibre { get; set; }
        public bool JuevesLibre { get; set; }

        // Días de la semana
        public string Viernes { get => _viernes; set { _viernes = value; OnPropertyChanged(); } }
        public string Sabado { get => _sabado; set { _sabado = value; OnPropertyChanged(); } }
        public string Domingo { get => _domingo; set { _domingo = value; OnPropertyChanged(); } }
        public string Lunes { get => _lunes; set { _lunes = value; OnPropertyChanged(); } }
        public string Martes { get => _martes; set { _martes = value; OnPropertyChanged(); } }
        public string Miercoles { get => _miercoles; set { _miercoles = value; OnPropertyChanged(); } }
        public string Jueves { get => _jueves; set { _jueves = value; OnPropertyChanged(); } }

        public string UltimaAct => DateTime.Now.ToString("HH:mm");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}