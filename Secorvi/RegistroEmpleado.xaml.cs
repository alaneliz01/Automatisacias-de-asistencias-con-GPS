using Secorvi.Models;
using System;
using System.Linq;
using System.Windows;

namespace Secorvi
{
    public partial class RegistroEmpleado : Window
    {
        public RegistroEmpleado()
        {
            InitializeComponent();

            // Tony: Generamos una sugerencia de matrícula basada en el tiempo actual
            // Esto evita que el usuario tenga que inventar una cada vez.
            txtMatricula.Text = "SEC-" + DateTime.Now.ToString("yyyyMMddHHss").Substring(8);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validaciones de campos obligatorios
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text) ||
                string.IsNullOrWhiteSpace(txtMatricula.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Password)) // Asumo que tienes un PasswordBox llamado txtPass
            {
                MessageBox.Show("Todos los campos, incluyendo la contraseña, son obligatorios.", "Datos Incompletos");
                return;
            }

            string matriculaNueva = txtMatricula.Text.Trim().ToUpper();

            // 2. Validación de Integridad: Evitar matrículas duplicadas
            bool existe = DataService.Empleados.Any(emp => emp.Matricula == matriculaNueva);
            if (existe)
            {
                MessageBox.Show("La matrícula ingresada ya pertenece a otro agente.", "Error de Duplicidad");
                return;
            }

            // 3. Crear el objeto con la estructura normalizada
            Empleado nuevo = new Empleado
            {
                Matricula = matriculaNueva,
                Nombre = txtNombre.Text.Trim().ToUpper(),
                Telefono = txtTelefono.Text.Trim(),
                Password = txtPass.Password, // Contraseña inicial para acceso o validación futura

                // Tony: Valores iniciales por defecto
                PuntoServicio = "PENDIENTE",
                TurnoTipo = "OPERATIVO",
                EsAdmin = false,
                EsSuperAdmin = false,
                ColorStatus = "#4B5563" // Gris inicial (Sin reporte)
            };

            try
            {
                // 4. Guardar a través del servicio persistente
                DataService.GuardarNuevoEmpleado(nuevo);

                // Tony: Indicamos al Panel de Control que la operación fue exitosa para que refresque la lista
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error técnico al guardar en el JSON: {ex.Message}", "Error Crítico");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}