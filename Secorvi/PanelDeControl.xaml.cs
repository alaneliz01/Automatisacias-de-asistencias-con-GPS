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
                DataService.ActualizarTodo();

                FiltrarYMostrar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SECORVI_LOG: Error en refresco: " + ex.Message);
            }
        }

        private void FiltrarYMostrar()
        {
            string filtro = txtBusqueda.Text?.Trim().ToLower() ?? "";
            var filtrados = DataService.Empleados.Where(x =>
                x.id_empleado.ToString().Contains(filtro) ||
                (x.nombre_completo?.ToLower().Contains(filtro) ?? false) ||
                (x.telefono?.Contains(filtro) ?? false)
            ).ToList();

            dgEmpleados.ItemsSource = null;
            dgEmpleados.ItemsSource = filtrados;

            if (lblTotal != null)
                lblTotal.Text = $"Agentes Activos: {filtrados.Count}";
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e) => FiltrarYMostrar();

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

        private void BtnRoles_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                GestionPermisos ventanaPermisos = new GestionPermisos(emp);
                ventanaPermisos.Owner = Window.GetWindow(this);

                if (ventanaPermisos.ShowDialog() == true)
                {
                    int nuevoRol = ventanaPermisos.IdRolSeleccionado;

                    DataService.CambiarPermisos(emp.id_empleado, nuevoRol);

                    System.Diagnostics.Debug.WriteLine($"SECORVI_LOG: Rol de {emp.nombre_completo} actualizado a ID {nuevoRol}");

                    CargarDatosDesdeDB();
                }
            }
            else
            {
                MessageBox.Show("Por favor, seleccione un agente de la lista primero.", "AVISO");
            }
        }
        private void BtnAsignacion_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var emp = btn?.DataContext as Empleado;

            if (emp != null)
            {
                Mapa ventanaMapa = new Mapa(emp.id_empleado, DateTime.Today);
                if (ventanaMapa.ShowDialog() == true)
                {
                    DataService.ActualizarTodo();
                    dgEmpleados.ItemsSource = null;
                    dgEmpleados.ItemsSource = DataService.Empleados;
                    lblTotal.Text = $"Agentes: {DataService.Empleados.Count}";
                }
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