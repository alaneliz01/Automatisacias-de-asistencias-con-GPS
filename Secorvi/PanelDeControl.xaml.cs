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

            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Stop();
            }
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
            {
                _autoRefreshTimer.Start();
            }
        }

        private void CargarDatosDesdeDB()
        {
            try
            {
                DataService.ActualizarTodo();
                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = DataService.Empleados;

                int totalAsistencias = DataService.Empleados.Count(x => x.CumplioAsistenciaHoy);
                ActualizarContador(DataService.Empleados.Count, totalAsistencias);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en refresh: " + ex.Message);
            }
        }

        private void ActualizarContador(int total, int asistencias)
        {
            if (lblTotal != null)
                lblTotal.Text = $"AGENTES ACTIVOS: {total} | ASISTENCIAS HOY: {asistencias}";
        }



        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBusqueda.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(filtro))
            {
                dgEmpleados.ItemsSource = DataService.Empleados;
                ActualizarContador(DataService.Empleados.Count, DataService.Empleados.Count(x => x.CumplioAsistenciaHoy));
                return;
            }

            var filtrados = DataService.Empleados.Where(x =>
                (x.Nombre?.ToLower().Contains(filtro) ?? false) ||
                (x.Apellido?.ToLower().Contains(filtro) ?? false) ||
                (x.Matricula?.ToLower().Contains(filtro) ?? false)
            ).ToList();

            dgEmpleados.ItemsSource = filtrados;
            ActualizarContador(filtrados.Count, filtrados.Count(x => x.CumplioAsistenciaHoy));
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            RegistroEmpleado ventanaRegistro = new RegistroEmpleado();
            ventanaRegistro.Owner = Window.GetWindow(this);
            if (ventanaRegistro.ShowDialog() == true) CargarDatosDesdeDB();
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                if (SesionActual.Usuario?.Id == emp.Id)
                {
                    MessageBox.Show("PROTOCOL DENIED: No puede desactivar su propio perfil.", "SECURITY ALERT");
                    return;
                }

                if (MessageBox.Show($"¿DESACTIVAR AGENTE {emp.Matricula}?", "CONFIRMAR", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DataService.EliminarEmpleado(emp.Id);
                    CargarDatosDesdeDB();
                }
            }
        }

        private void BtnRoles_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                DataService.CambiarPermisos(emp.Id, !emp.EsAdmin);
                CargarDatosDesdeDB();
            }
        }

        private void BtnCalendario_Click(object sender, RoutedEventArgs e) => AbrirCalendarioSeleccionado();
        private void DgEmpleados_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => AbrirCalendarioSeleccionado();

        private void AbrirCalendarioSeleccionado()
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
                this.NavigationService?.Navigate(new CalendarioEmpleado(emp));
        }
    }
}