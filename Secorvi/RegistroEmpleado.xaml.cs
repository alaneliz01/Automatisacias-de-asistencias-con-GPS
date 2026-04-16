using Secorvi.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Secorvi
{
    // Cambiamos 'Page' por 'Window'
    public partial class RegistroEmpleado : Window
    {
        public RegistroEmpleado()
        {
            InitializeComponent();
            // Genera matrícula automática basada en tiempo real
            txtMatricula.Text = "SEC-" + DateTime.Now.ToString("HHmmss");
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Password))
            {
                MessageBox.Show("SISTEMA: Complete todos los campos obligatorios.", "ADVERTENCIA");
                return;
            }

            try
            {
                var nuevoEmpleado = new Empleado
                {
                    Nombre = txtNombre.Text.Trim().ToUpper(),
                    Apellido = txtApellido.Text.Trim().ToUpper(),
                    Matricula = txtMatricula.Text,
                    Telefono = txtTelefono.Text.Trim(),
                    // Generamos un usuario sugerido (Nombre + segundos)
                    Usuario = txtNombre.Text.Split(' ')[0].ToLower() + DateTime.Now.Second.ToString(),
                    Contrasena = txtPass.Password,
                    Rol = "AGENTE", // Usamos el campo Rol que lee tu DataService
                    Activo = true
                };

                DataService.AgregarEmpleado(nuevoEmpleado);

                MessageBox.Show("AGENTE REGISTRADO EXITOSAMENTE", "OPERACIÓN TÁCTICA");


                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("FALLO DE SISTEMA: " + ex.Message, "ERROR CRÍTICO");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}