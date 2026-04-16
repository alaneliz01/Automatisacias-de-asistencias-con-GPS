using Secorvi.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Secorvi
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            InicializarDatos();
        }

        private void InicializarDatos()
        {
            try
            {
                // Cargamos todo desde MySQL al arrancar
                DataService.ActualizarTodo();

                if (DataService.Empleados.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("LOG: La base de datos está vacía o no hay conexión.");
                }
            }
            catch (Exception ex)
            {
                // Si la conexión a MySQL falla aquí, lo verás en el brdStatus del XAML
                MostrarAviso("ERROR DE CONEXIÓN: " + ex.Message, "#F8D7DA", "#721C24");
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string userDigitado = txtUser.Text.Trim();
            string passDigitada = txtPass.Password.Trim();

            if (string.IsNullOrEmpty(userDigitado) || string.IsNullOrEmpty(passDigitada))
            {
                MostrarAviso("CRITICAL: Ingrese usuario y contraseña", "#FFF3CD", "#856404");
                return;
            }

            // IMPORTANTE: Forzamos una recarga antes de buscar para asegurar que 
            // si acabas de registrar a alguien, ya aparezca aquí.
            DataService.CargarEmpleados();

            // Buscamos coincidencia en la lista cargada de MySQL
            // Usamos .FirstOrDefault() que es más estándar en LINQ
            var encontrado = DataService.Empleados.FirstOrDefault(x =>
                x.Usuario.Equals(userDigitado, StringComparison.OrdinalIgnoreCase) &&
                x.Contrasena == passDigitada);

            if (encontrado != null)
            {
                if (!encontrado.Activo)
                {
                    MostrarAviso("ACCESO DENEGADO: Usuario inactivo", "#F8D7DA", "#721C24");
                    return;
                }

                // Guardamos en la clase estática que verificamos antes
                SesionActual.Usuario = encontrado;

                ContenedorPrincipal principal = new ContenedorPrincipal();
                principal.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show($"ACCESO DENEGADO.\nVerifique sus credenciales.\nAgentes en DB: {DataService.Empleados.Count}", "SEGURIDAD SECORVI");
            }
        }

        private void MostrarAviso(string mensaje, string colorFondo, string colorTexto)
        {
            var bc = new BrushConverter();
            if (brdStatus != null)
            {
                brdStatus.Background = (Brush)bc.ConvertFrom(colorFondo);
                txtStatusMsg.Text = mensaje.ToUpper();
                txtStatusMsg.Foreground = (Brush)bc.ConvertFrom(colorTexto);
                brdStatus.Visibility = Visibility.Visible;
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}