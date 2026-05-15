using System;
using System.Windows;
using System.Threading.Tasks;

namespace Secorvi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Manejador de errores global para evitar cierres inesperados en campo
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 2. Carga de datos inicial
            // Intentamos cargar, pero si falla la DB, permitimos que abra el Login 
            // para que el usuario pueda revisar su conexión.
            Task.Run(() => {
                try
                {
                    DataService.ActualizarTodo();
                }
                catch (Exception ex)
                {
                    // Log silencioso o aviso posterior
                    Console.WriteLine("Error inicial de datos: " + ex.Message);
                }
            });
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // En lugar de crashear, mostramos un mensaje estilo terminal
            MessageBox.Show($"CRITICAL SYSTEM ERROR:\n{e.Exception.Message}",
                            "SECORVI CORE FAILURE",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

            // Si quieres que la app intente seguir viva:
            e.Handled = true;
        }

    }
}