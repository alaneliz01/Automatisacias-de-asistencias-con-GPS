using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Secorvi
{
    public partial class ConfirmarEliminacion : Window
    {
        private readonly string _nombreEsperado;
        public bool ResultadoValidacion { get; private set; }

        public ConfirmarEliminacion(string nombreEmpleado)
        {
            InitializeComponent();
            _nombreEsperado = nombreEmpleado;
            ResultadoValidacion = false;

            // Tony: Mostramos al usuario qué es lo que debe escribir exactamente
            lblInstruccion.Text = $"Para eliminar, escriba el nombre completo del agente:";
            lblNombreAgente.Text = _nombreEsperado;

            // Inicializamos el botón como deshabilitado por seguridad
            btnEliminar.IsEnabled = false;
            btnEliminar.Opacity = 0.5;
        }

        private void TxtNombreConfirmar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Tony: Validación en tiempo real para habilitar el botón
            bool coinciden = txtNombreConfirmar.Text.Trim().Equals(_nombreEsperado, StringComparison.OrdinalIgnoreCase);

            btnEliminar.IsEnabled = coinciden;
            btnEliminar.Opacity = coinciden ? 1.0 : 0.5;

            // Feedback visual opcional: cambia el borde si coincide
            txtNombreConfirmar.BorderBrush = coinciden ? Brushes.Green : Brushes.Red;
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            // Re-validación final antes de cerrar
            if (txtNombreConfirmar.Text.Trim().Equals(_nombreEsperado, StringComparison.OrdinalIgnoreCase))
            {
                ResultadoValidacion = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("El nombre no coincide exactamente.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}