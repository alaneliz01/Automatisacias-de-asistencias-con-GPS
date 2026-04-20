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

            // 1. Matrícula técnica: Se queda para no romper tus modelos de C#
            txtMatricula.Text = "SEC-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // 2. ID Maestro: El que dictarás para el Chatbot de WhatsApp
            lblIdGenerado.Text = DataService.ObtenerProximoIdEmpleado().ToString();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validación de campos requeridos por la lógica de negocio y la DB
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Password) ||
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

                // 3. Generación de Usuario: Mantenemos 'Rommel style' para el login de escritorio
                string usuarioAuto = (nombre.Split(' ')[0].ToLower() + DateTime.Now.ToString("ss"));

                DataService.ActualizarTodo();

                // Validación de integridad para el número de teléfono
                if (DataService.Empleados.Any(x => x.telefono == tel))
                {
                    MessageBox.Show("ERROR: El número de teléfono ya está registrado.", "DUPLICADO", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                // 4. Mapeo al Modelo: Llenamos todos los campos para evitar nulos en la DB
                var nuevoEmpleado = new Empleado
                {
                    nombre_completo = $"{nombre} {apellido}".ToUpper(),
                    telefono = tel,
                    usuario = usuarioAuto,
                    matricula = txtMatricula.Text,
                    contrasena = txtPass.Password,
                    id_rol = int.Parse(lblIdRol.Text), // Generalmente Rol 3 para Agentes
                    estatus = "Activo"
                };

                // 5. Persistencia: DataService se encarga del INSERT
                DataService.AgregarEmpleado(nuevoEmpleado);

                // Feedback visual con la información crítica para el Administrador
                MessageBox.Show($"¡REGISTRO EXITOSO!\n\n" +
                                $"DATOS PARA PANEL (C#):\n" +
                                $"Usuario: {usuarioAuto}\n\n" +
                                $"DATOS PARA BOT (WhatsApp):\n" +
                                $"ID Único: {lblIdGenerado.Text}",
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