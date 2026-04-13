using Secorvi.Models;
using System;
using System.Windows;
// esto es una ventana para registrar un nuevo empleado,
namespace Secorvi
{
    public partial class RegistroEmpleado : Window
    {
        public RegistroEmpleado(int proximoId)
        {
            InitializeComponent();
            txtMatricula.Text = $"SEC-{proximoId:D3}";
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtTelefono.Text)) return;

            Empleado nuevo = new Empleado
            {
                Matricula = txtMatricula.Text,
                Nombre = txtNombre.Text.Trim().ToUpper(),
                Telefono = txtTelefono.Text.Trim()
            };

            DataService.GuardarNuevoEmpleado(nuevo);
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}