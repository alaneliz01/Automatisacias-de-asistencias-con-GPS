using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
// esto es la ventana de login, la cual se encarga de validar las credenciales del usuario
//modificaremos luego

namespace Secorvi
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUser.Text.Trim();
            string password = txtPass.Password;

            if (usuario == "Rommel" && password == "1234")
            {
                MostrarAviso("ACCESO CONCEDIDO - BIENVENIDO COMANDANTE", "#2D5A27", "#88FF88");
                await Task.Delay(1000);

                ContenedorPrincipal principal = new ContenedorPrincipal();

                principal.MainFrame.Navigate(new PanelDeControl());

                principal.Show();
                this.Close();
            }
            else
            {
                MostrarAviso("ERROR: CREDENCIALES NO VÁLIDAS", "#5A2727", "#FF8888");
            }
        }

        private void MostrarAviso(string mensaje, string colorFondo, string colorTexto)
        {
            try
            {
                var bc = new BrushConverter();
                brdStatus.Background = (Brush)bc.ConvertFrom(colorFondo);
                txtStatusMsg.Text = mensaje;
                txtStatusMsg.Foreground = (Brush)bc.ConvertFrom(colorTexto);
                brdStatus.Visibility = Visibility.Visible;
            }
            catch { brdStatus.Visibility = Visibility.Visible; }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}