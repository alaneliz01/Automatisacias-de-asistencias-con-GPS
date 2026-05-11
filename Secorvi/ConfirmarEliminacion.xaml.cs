using System;
using System.Windows;
using System.Windows.Controls;

namespace Secorvi
{
    public partial class ConfirmarEliminacion : Window
    {
        public bool ResultadoValidacion { get; private set; } = false;
        private string nombreEsperado;

        public ConfirmarEliminacion(string nombreAgente)
        {
            InitializeComponent();
            nombreEsperado = nombreAgente;
            lblNombreAgente.Text = nombreEsperado;

            // Pone el cursor automáticamente en la caja de texto
            txtNombreConfirmar.Focus();
        }

        private void TxtNombreConfirmar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Compara el texto ignorando mayúsculas y minúsculas
            if (string.Equals(txtNombreConfirmar.Text.Trim(), nombreEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                btnEliminar.IsEnabled = true;
                btnEliminar.Opacity = 1;
            }
            else
            {
                btnEliminar.IsEnabled = false;
                btnEliminar.Opacity = 0.5;
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            ResultadoValidacion = true;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ResultadoValidacion = false;
            this.DialogResult = false;
            this.Close();
        }
    }
}