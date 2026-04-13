using System;
using System.Windows;

namespace Secorvi
{
    public partial class SeleccionarFechaLibre : Window
    {
        public DateTime FechaSeleccionada { get; private set; }

        public SeleccionarFechaLibre()
        {
            InitializeComponent();
            calFecha.SelectedDate = DateTime.Now; // Por defecto hoy
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (calFecha.SelectedDate.HasValue)
            {
                FechaSeleccionada = calFecha.SelectedDate.Value;
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}