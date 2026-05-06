using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Secorvi
{
    public partial class PanelDeControl : Page
    {
        private DispatcherTimer _autoRefreshTimer;
        private ICollectionView _empleadosView;

        public PanelDeControl()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                CargarDatosDesdeDB();
            };
        }

        private void PanelDeControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDatosDesdeDB();
            ConfigurarAutoRefresco();
        }

        private void PanelDeControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _autoRefreshTimer?.Stop();
        }

        private void ConfigurarAutoRefresco()
        {
            if (_autoRefreshTimer == null)
            {
                _autoRefreshTimer = new DispatcherTimer();
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(30);
                _autoRefreshTimer.Tick += (s, ev) => CargarDatosDesdeDB();
            }

            if (!_autoRefreshTimer.IsEnabled)
                _autoRefreshTimer.Start();
        }

        private async void CargarDatosDesdeDB()
        {
            try
            {
                // 1. Refrescamos toda la información desde MySQL en segundo plano
                await Task.Run(() => DataService.ActualizarTodo());

                // 2. Obtenemos la vista de la colección
                _empleadosView = CollectionViewSource.GetDefaultView(DataService.Empleados);

                // 3. Configuramos el filtro (mantenemos tu lógica de búsqueda)
                _empleadosView.Filter = (obj) =>
                {
                    if (obj is Empleado emp)
                    {
                        string filtro = txtBusqueda.Text?.Trim().ToLower() ?? "";
                        if (string.IsNullOrEmpty(filtro)) return true;

                        return (emp.nombre_completo?.ToLower().Contains(filtro) ?? false) ||
                                emp.id_empleado.ToString().Contains(filtro) ||
                                (emp.telefono?.Contains(filtro) ?? false);
                    }
                    return false;
                };

                // --- CAMBIO CLAVE AQUÍ ---
                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = _empleadosView;

                // Refrescamos la vista de colección
                _empleadosView.Refresh();

                ActualizarContadorUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SECORVI_LOG_ERROR: Fallo en refresco: {ex.Message}");
            }
        }

        private void ActualizarContadorUI()
        {
            if (lblTotal != null && _empleadosView != null)
            {
                
                int count = _empleadosView.Cast<object>().Count();
                lblTotal.Text = $"Agentes Activos: {count}";
            }
        }

        // Evento de búsqueda optimizado: solo refresca la vista existente
        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            _empleadosView?.Refresh();
            ActualizarContadorUI();
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            RegistroEmpleado ventanaRegistro = new RegistroEmpleado { Owner = Window.GetWindow(this) };

            // Si la ventana se cierra con éxito al dar en Guardar
            if (ventanaRegistro.ShowDialog() == true)
            {
                // 1. Recargamos los datos en tu tabla para que aparezca el nuevo
                CargarDatosDesdeDB();

                // 2. Extraemos el ID del empleado que la ventana acaba de crear
                int idNuevo = ventanaRegistro.IdEmpleadoGenerado;

                // 3. Preparamos las fechas (el día de hoy por defecto)
                var fechas = new System.Collections.Generic.List<DateTime> { DateTime.Now.Date };

                // 4. Navegamos directamente a la página Mapa (Asignar)
                this.NavigationService?.Navigate(new Mapa(idNuevo, fechas));
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                if (SesionActual.Usuario?.id_empleado == emp.id_empleado)
                {
                    MessageBox.Show("ACCESO DENEGADO: No puede dar de baja su propio acceso.", "SEGURIDAD");
                    return;
                }

                var res = MessageBox.Show($"¿CONFIRMAR BAJA LÓGICA DEL AGENTE {emp.nombre_completo}?\n\n" +
                    "Estatus cambiará a 'Inactivo' y no podrá usar el chatbot.",
                    "OPERACIÓN DE BAJA", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (res == MessageBoxResult.Yes)
                {
                    DataService.EliminarEmpleado(emp.id_empleado);
                    CargarDatosDesdeDB();
                }
            }
        }

        
        private void BtnAsignacion_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Empleado emp)
            {
                // Creamos una lista que contenga solo la fecha de hoy
                var fechas = new List<DateTime> { DateTime.Today };

                // Pasamos la lista al constructor
                Mapa paginaMapa = new Mapa(emp.id_empleado, fechas);

                // Navegamos
                this.NavigationService?.Navigate(paginaMapa);
            }
        }

        private void BtnCalendario_Click(object sender, RoutedEventArgs e) =>
            AbrirCalendarioSeleccionado();

        private void DgEmpleados_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            AbrirCalendarioSeleccionado();

        private void AbrirCalendarioSeleccionado()
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                this.NavigationService?.Navigate(new CalendarioEmpleado(emp));
            }
        }

        private void BtnTurnos_Click(object sender, RoutedEventArgs e)
        {
                    }
    }
}