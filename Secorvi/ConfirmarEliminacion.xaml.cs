using System;
using System.Windows;

namespace Secorvi
{
    public partial class ConfirmarEliminacion : Window
    {
        private string _nombreEsperado;
        public bool ResultadoValidacion { get; private set; }

        public ConfirmarEliminacion(string nombreEmpleado)
        {
            InitializeComponent();
            _nombreEsperado = nombreEmpleado;
            ResultadoValidacion = false;
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            // Validamos que el nombre coincida ignorando mayúsculas/minúsculas para evitar frustración del usuario
            if (txtNombreConfirmar.Text.Trim().Equals(_nombreEsperado, StringComparison.OrdinalIgnoreCase))
            {
                ResultadoValidacion = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show($"El nombre ingresado no coincide con '{_nombreEsperado}'.\nVerifique espacios y acentos.",
                                "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
    }
}