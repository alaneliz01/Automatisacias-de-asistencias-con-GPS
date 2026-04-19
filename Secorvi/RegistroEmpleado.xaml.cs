using Secorvi.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Secorvi
{
    public partial class RegistroEmpleado : Window
    {
        public RegistroEmpleado()
        {
            InitializeComponent();
            // Matrícula única: SEC-20260418...
            txtMatricula.Text = "SEC-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // VALIDACIÓN DE CAMPOS
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Password) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("SISTEMA: Todos los campos son obligatorios.", "OCC LOG", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string nombre = txtNombre.Text.Trim();
                string apellido = txtApellido.Text.Trim();

                // Generamos un usuario corto para n8n/Bot: ejemplo "alan4522"
                string primerNombre = nombre.Contains(" ") ? nombre.Split(' ')[0] : nombre;
                string usuarioSugerido = (primerNombre.ToLower() + DateTime.Now.ToString("ssmm"));

                // ADAPTACIÓN: Propiedades en minúsculas para coincidir con la DB y borrar errores
                var nuevoEmpleado = new Empleado
                {
                    nombre_completo = $"{nombre} {apellido}".ToUpper(),
                    matricula = txtMatricula.Text,
                    telefono = txtTelefono.Text.Trim(),
                    usuario = usuarioSugerido,
                    contrasena = txtPass.Password,
                    id_rol = 3, // Rango Agente
                    estatus = "Activo"
                };

                // PERSISTENCIA
                DataService.AgregarEmpleado(nuevoEmpleado);

                MessageBox.Show($"OPERACIÓN EXITOSA\n\nUsuario: {usuarioSugerido}\nMatrícula: {nuevoEmpleado.matricula}",
                                "OCC SYSTEM", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR DE ENLACE SQL: " + ex.Message, "CRÍTICO", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}