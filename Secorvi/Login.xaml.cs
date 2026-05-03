using Secorvi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Secorvi
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            _ = InicializarSistemaAsync();
        }

        // --- GESTIÓN DE VENTANA ---

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void BtnSalir_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // --- LÓGICA DE SISTEMA ---

        private async Task InicializarSistemaAsync()
        {
            try
            {
                await Task.Run(() => DataService.ActualizarTodo());
            }
            catch
            {
                MostrarAviso("SYSTEM_ERROR: FALLO DE CONEXIÓN DB", "#3D1B1E", "#FF5252");
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Password.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MostrarAviso("INPUT_REQUIRED: LLENE TODOS LOS CAMPOS", "#332B00", "#FFB300");
                return;
            }

            // --- CÓDIGO NUEVO: Candado para evitar choque de tiempos ---
            if (DataService.Empleados == null || DataService.Empleados.Count == 0)
            {
                MostrarAviso("SYSTEM_WAIT: CARGANDO DATOS DB...", "#332B00", "#FFB300");
                return;
            }
            // -----------------------------------------------------------

            // Buscamos al usuario en la base de datos (DataService)
            var usuario = DataService.Empleados.FirstOrDefault(x =>
                x.usuario.Equals(user, StringComparison.OrdinalIgnoreCase) &&
                x.contrasena == pass);

            if (usuario != null)
            {
                // Verificamos si el agente está activo en la plataforma SECORVI
                if (!usuario.estatus.Equals("Activo", StringComparison.OrdinalIgnoreCase))
                {
                    MostrarAviso("ACCESS_DENIED: CUENTA INACTIVA", "#3D1B1E", "#FF5252");
                    return;
                }

                // Verificamos privilegios de acceso al Command Center (Admin o Supervisor)
                if (usuario.id_rol == 1 || usuario.id_rol == 2)
                {
                    // 1. Registramos la sesión globalmente
                    SesionActual.Usuario = usuario;

                    // 2. CORRECCIÓN: Pasamos el objeto 'usuario' al constructor del Contenedor
                    ContenedorPrincipal principal = new ContenedorPrincipal(usuario);
                    principal.Show();

                    // 3. Cerramos el módulo de autenticación
                    this.Close();
                }
                else
                {
                    MessageBox.Show("ACCESO DENEGADO: Tu rol de AGENTE no permite el acceso a esta terminal de escritorio. Use la interfaz de WhatsApp.",
                                    "SEGURIDAD SECORVI", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            }
            else
            {
                MostrarAviso("AUTH_FAILURE: CREDENCIALES INVÁLIDAS", "#3D1B1E", "#FF5252");
            }
        }
        private void MostrarAviso(string mensaje, string colorHexFondo, string colorHexTexto)
        {
            var converter = new BrushConverter();
            brdStatus.Background = (Brush)converter.ConvertFrom(colorHexFondo);
            txtStatusMsg.Text = mensaje;
            txtStatusMsg.Foreground = (Brush)converter.ConvertFrom(colorHexTexto);
            brdStatus.Visibility = Visibility.Visible;
        }
    }
}