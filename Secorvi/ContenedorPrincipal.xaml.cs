using System;
using System.Windows;
using Secorvi.Models;

namespace Secorvi
{
    /// <summary>
    /// Lógica de interacción para el Centro de Comando de SECORVI.
    /// Maneja la navegación persistente y el estado de la sesión del operador.
    /// </summary>
    public partial class ContenedorPrincipal : Window
    {
        // Almacenamos el objeto del empleado para persistencia de sesión y auditoría
        private readonly Empleado _operadorActivo;

        public ContenedorPrincipal(Empleado operador)
        {
            InitializeComponent();

            // 1. Inicialización de Estado
            _operadorActivo = operador ?? throw new ArgumentNullException(nameof(operador));

            // 2. Configuración de Interfaz
            ConfigurarSesionUI();

            // 3. Carga de Módulo Inicial
            MainFrame.Navigate(new PanelDeControl());
        }

        private void ConfigurarSesionUI()
        {
            // Actualiza el indicador visual del operador en la barra superior
            if (lblNombreUsuario != null)
            {
                lblNombreUsuario.Text = _operadorActivo.nombre_completo.ToUpper();
            }
        }

        #region Navegación de Módulos

        private void NavPanel_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PanelDeControl());
        }

        private void NavTurnos_Click(object sender, RoutedEventArgs e)
        {
            // Vista de planeación semanal
            MainFrame.Navigate(new Turnos());
        }

    

        private void NavSeguridad_Click(object sender, RoutedEventArgs e)
        {
            // Módulo de Auditoría: Registro histórico de firmas y cambios de nivel
            MessageBox.Show($"OPERADOR: {_operadorActivo.usuario}\n" +
                            "Módulo de Auditoría: Registro de firmas y autorizaciones encriptadas.",
                            "SECORVI SECURITY LOG", MessageBoxButton.OK, MessageBoxImage.Information);
            // MainFrame.Navigate(new LogSeguridad());
        }

        #endregion

        #region Gestión de Sesión

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("¿Confirmar cierre de sesión y salida del Command Center?",
                                    "TERMINAR SESIÓN", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                // Reinicia el ciclo de autenticación
                Login ventanaLogin = new Login();
                ventanaLogin.Show();
                this.Close();
            }
        }

        #endregion

        #region Controles de Ventana Tácticos

        private void BtnMax_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized)
                               ? WindowState.Normal
                               : WindowState.Maximized;
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Apagado forzoso de todos los procesos del sistema
            Application.Current.Shutdown();
        }

        #endregion
    }
}