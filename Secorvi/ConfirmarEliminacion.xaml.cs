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

            _nombreEsperado = nombreEmpleado ?? "";
            ResultadoValidacion = false;

            // Texto UI
            lblInstruccion.Text = "Para eliminar, escriba el nombre completo del agente:";
            lblNombreAgente.Text = _nombreEsperado;

            // Estado inicial seguro
            btnEliminar.IsEnabled = false;
            btnEliminar.Opacity = 0.5;

            txtNombreConfirmar.BorderBrush = Brushes.Gray;
        }

        private void TxtNombreConfirmar_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidarEntrada();
        }

        private void ValidarEntrada()
        {
            string input = txtNombreConfirmar.Text?.Trim() ?? "";

            bool coinciden = input.Equals(_nombreEsperado, StringComparison.OrdinalIgnoreCase);

            btnEliminar.IsEnabled = coinciden;
            btnEliminar.Opacity = coinciden ? 1.0 : 0.5;

            // Feedback visual
            if (string.IsNullOrWhiteSpace(input))
                txtNombreConfirmar.BorderBrush = Brushes.Gray;
            else
                txtNombreConfirmar.BorderBrush = coinciden ? Brushes.Green : Brushes.Red;
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            string input = txtNombreConfirmar.Text?.Trim() ?? "";

            if (input.Equals(_nombreEsperado, StringComparison.OrdinalIgnoreCase))
            {
                ResultadoValidacion = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "El nombre no coincide exactamente.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ResultadoValidacion = false;
            DialogResult = false;
            Close();
        }
    }
}