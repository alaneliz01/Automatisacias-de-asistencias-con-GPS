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
            lblIdGenerado.Text = DataService.ObtenerProximoIdEmpleado().ToString();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validación de campos requeridos (sin contraseña)
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("SISTEMA: Todos los campos son obligatorios.", "SECORVI LOG", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string nombre = txtNombre.Text.Trim();
                string apellido = txtApellido.Text.Trim();
                string tel = txtTelefono.Text.Trim();

                string usuarioAuto = (nombre.Split(' ')[0].ToLower() + DateTime.Now.ToString("ss"));

                DataService.ActualizarTodo();

                if (DataService.Empleados.Any(x => x.telefono == tel))
                {
                    MessageBox.Show("ERROR: El número de teléfono ya está registrado.", "DUPLICADO", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                // Mapeo al Modelo (contraseña nula por defecto al ser agente)
                var nuevoEmpleado = new Empleado
                {
                    nombre_completo = $"{nombre} {apellido}".ToUpper(),
                    telefono = tel,
                    usuario = usuarioAuto,
                    contrasena = null,
                    id_rol = int.Parse(lblIdRol.Text),
                    estatus = "Activo",
                    matricula = "SEC-" + lblIdGenerado.Text
                };

                DataService.AgregarEmpleado(nuevoEmpleado);

                // Feedback visual sin mostrar contraseña
                MessageBox.Show($"¡REGISTRO EXITOSO!\n\n" +
                                $"DATOS DEL AGENTE:\n" +
                                $"Usuario: {usuarioAuto}\n" +
                                $"ID Agente: {lblIdGenerado.Text}\n\n" +
                                $"Nota: El acceso al sistema táctico requiere asignación de rol gerencial.",
                                "SECORVI SYSTEM", MessageBoxButton.OK, MessageBoxImage.Information);

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