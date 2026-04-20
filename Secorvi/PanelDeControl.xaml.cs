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
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(30); // Refresco cada 30 seg
                _autoRefreshTimer.Tick += (s, ev) => CargarDatosDesdeDB();
            }

            if (!_autoRefreshTimer.IsEnabled)
                _autoRefreshTimer.Start();
        }

        private void CargarDatosDesdeDB()
        {
            try
            {
                // 1. Sincroniza con MySQL (Solo trae los 'Activos' según tu DataService)
                DataService.ActualizarTodo();

                // 2. Aplicamos el filtro actual para no perder lo que el usuario está escribiendo
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

            // BÚSQUEDA INTELIGENTE: Por ID, Nombre, Matrícula o Teléfono
            var filtrados = DataService.Empleados.Where(x =>
                x.id_empleado.ToString().Contains(filtro) ||
                (x.nombre_completo?.ToLower().Contains(filtro) ?? false) ||
                (x.matricula?.ToLower().Contains(filtro) ?? false) ||
                (x.telefono?.Contains(filtro) ?? false)
            ).ToList();

            dgEmpleados.ItemsSource = null;
            dgEmpleados.ItemsSource = filtrados;

            // Actualizamos el contador visual
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
                // Seguridad: No borrar al administrador actual
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
                // Cambia entre Agente (3) y Admin Empleados (2)
                int nuevoRol = (emp.id_rol == 3) ? 2 : 3;
                string nombreRol = (nuevoRol == 2) ? "ADMINISTRADOR" : "AGENTE";

                var res = MessageBox.Show($"¿CAMBIAR ACCESO DE {emp.nombre_completo} A {nombreRol}?",
                    "GESTIÓN DE PERMISOS", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    DataService.CambiarPermisos(emp.id_empleado, nuevoRol);
                    CargarDatosDesdeDB();
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