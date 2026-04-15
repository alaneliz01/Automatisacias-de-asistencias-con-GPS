using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Secorvi.Models;

namespace Secorvi
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            // Aseguramos carga de datos al iniciar
            DataService.CargarTodo();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuarioInput = txtUser.Text.Trim();
            string passwordInput = txtPass.Password;

            if (string.IsNullOrWhiteSpace(usuarioInput) || string.IsNullOrWhiteSpace(passwordInput))
            {
                MostrarAviso("INGRESE SUS CREDENCIALES", "#5A5A27", "#FFFF88");
                return;
            }

            // Búsqueda en DataService
            var userLogueado = DataService.Empleados.FirstOrDefault(u =>
                (u.Nombre.Equals(usuarioInput, StringComparison.OrdinalIgnoreCase) ||
                 u.Matricula.Equals(usuarioInput, StringComparison.OrdinalIgnoreCase))
                && u.Password == passwordInput);

            if (userLogueado != null)
            {
                if (userLogueado.EsAdmin)
                {
                    SesionActual.Usuario = userLogueado;
                    MostrarAviso($"ACCESO CONCEDIDO - HOLA {userLogueado.Nombre.Split(' ')[0].ToUpper()}", "#2D5A27", "#88FF88");

                    await Task.Delay(800);
                    ContenedorPrincipal principal = new ContenedorPrincipal();
                    principal.Show();
                    principal.MainFrame.Navigate(new PanelDeControl());
                    this.Close();
                }
                else
                {
                    MostrarAviso("ERROR: SIN RANGO DE JEFE", "#5A2727", "#FF8888");
                }
            }
            else
            {
                MostrarAviso("ERROR: CREDENCIALES INCORRECTAS", "#5A2727", "#FF8888");
            }
        }

        private void MostrarAviso(string mensaje, string colorFondo, string colorTexto)
        {
            var bc = new BrushConverter();
            brdStatus.Background = (Brush)bc.ConvertFrom(colorFondo);
            txtStatusMsg.Text = mensaje;
            txtStatusMsg.Foreground = (Brush)bc.ConvertFrom(colorTexto);
            brdStatus.Visibility = Visibility.Visible;
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}