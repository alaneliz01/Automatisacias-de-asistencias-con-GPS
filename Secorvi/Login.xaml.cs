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

                DataService.ActualizarTodo();

                if (DataService.Empleados.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("LOG: La base de datos está vacía o no hay conexión.");
                }
            }
            catch (Exception ex)
            {
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