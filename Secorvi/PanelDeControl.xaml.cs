using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Secorvi
{
    public partial class PanelDeControl : Page
    {
        private DispatcherTimer _autoRefreshTimer;

        public PanelDeControl()
        {
            InitializeComponent();
            this.Loaded += PanelDeControl_Loaded;
            this.Unloaded += PanelDeControl_Unloaded;
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

        private void CargarDatosDesdeDB()
        {
            try
            {
                // Refresco masivo de datos desde MySQL
                DataService.ActualizarTodo();

                // Enlazamos al DataGrid (dgEmpleados)
                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = DataService.Empleados;

                // Actualizamos el contador de registros del diseño
                if (lblTotal != null)
                {
                    lblTotal.Text = $"Registros: {DataService.Empleados.Count}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OCC_LOG: Error en refresco automático: " + ex.Message);
            }
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBusqueda.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(filtro))
            {
                dgEmpleados.ItemsSource = DataService.Empleados;
                return;
            }

            // Búsqueda en los campos del modelo en minúsculas
            var filtrados = DataService.Empleados.Where(x =>
                (x.nombre_completo?.ToLower().Contains(filtro) ?? false) ||
                (x.matricula?.ToLower().Contains(filtro) ?? false) ||
                (x.telefono?.Contains(filtro) ?? false)
            ).ToList();

            dgEmpleados.ItemsSource = filtrados;
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            RegistroEmpleado ventanaRegistro = new RegistroEmpleado();
            ventanaRegistro.Owner = Window.GetWindow(this);
            if (ventanaRegistro.ShowDialog() == true)
                CargarDatosDesdeDB();
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                // Seguridad: No permitir que el admin activo se borre a sí mismo
                if (SesionActual.Usuario?.id_empleado == emp.id_empleado)
                {
                    MessageBox.Show("ACCESO DENEGADO: No puede dar de baja su propio acceso administrativo.", "SEGURIDAD SECORVI");
                    return;
                }

                var res = MessageBox.Show($"¿CONFIRMAR BAJA DEFINITIVA DEL AGENTE {emp.nombre_completo}?",
                    "OPERACIÓN DE BAJA", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (res == MessageBoxResult.Yes)
                {
                    DataService.EliminarEmpleado(emp.id_empleado);
                    CargarDatosDesdeDB();
                }
            }
        }

        private void BtnRoles_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                bool esAdminActual = (emp.id_rol == 1);
                string nuevoRol = esAdminActual ? "AGENTE" : "ADMINISTRADOR";

                var res = MessageBox.Show($"¿CAMBIAR NIVEL DE ACCESO DE {emp.nombre_completo} A {nuevoRol}?",
                    "GESTIÓN DE PERMISOS", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    DataService.CambiarPermisos(emp.id_empleado, !esAdminActual);
                    CargarDatosDesdeDB();
                }
            }
        }

        private void BtnMantenimiento_Click(object sender, RoutedEventArgs e)
        {
            if (!SesionActual.EsSuperAdmin) return;

            var res = MessageBox.Show("¿EJECUTAR PURGA DE REGISTROS ANTIGUOS (6 MESES)?",
                "MANTENIMIENTO DE SISTEMA", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (res == MessageBoxResult.Yes)
            {
                int filas = DataService.PurgarRegistrosAntiguos(6);
                MessageBox.Show($"OPTIMIZACIÓN FINALIZADA. {filas} REGISTROS ELIMINADOS.", "OCC_LOG");
                CargarDatosDesdeDB();
            }
        }

        private void BtnCalendario_Click(object sender, RoutedEventArgs e) => AbrirCalendarioSeleccionado();

        private void DgEmpleados_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => AbrirCalendarioSeleccionado();

        private void AbrirCalendarioSeleccionado()
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                this.NavigationService?.Navigate(new CalendarioEmpleado(emp));
            }
        }
    }
}