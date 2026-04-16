using Secorvi.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Secorvi
{
    public partial class RegistroEmpleado : Page
    {
        public RegistroEmpleado()
        {
            InitializeComponent();
            // Genera matrícula automática basada en tiempo real
            txtMatricula.Text = "SEC-" + DateTime.Now.ToString("HHmmss");
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validación de campos obligatorios
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Password))
            {
                MessageBox.Show("SISTEMA: Complete todos los campos antes de continuar.");
                return;
            }

            try
            {
                // 2. Crear el objeto con los datos del formulario
                var nuevoEmpleado = new Empleado
                {
                    Nombre = txtNombre.Text.ToUpper(),
                    Apellido = txtApellido.Text.ToUpper(),
                    Matricula = txtMatricula.Text,
                    Telefono = txtTelefono.Text,
                    Usuario = txtNombre.Text.Split(' ')[0].ToLower() + DateTime.Now.Second.ToString(),
                    Contrasena = txtPass.Password,
                    EsAdmin = false,
                    Activo = true
                };

                // 3. Guardar en MySQL a través del DataService
                DataService.AgregarEmpleado(nuevoEmpleado);

                MessageBox.Show("AGENTE REGISTRADO EXITOSAMENTE", "OPERACIÓN TÁCTICA");

                // 4. NAVEGACIÓN (Correcto para una Page)
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR AL REGISTRAR: " + ex.Message, "FALLO DE SISTEMA");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            // Regresar al Panel de Control sin guardar
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}