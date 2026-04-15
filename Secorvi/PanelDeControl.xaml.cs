using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Secorvi.Models;
using Microsoft.VisualBasic;

namespace Secorvi
{
    public partial class PanelDeControl : Page
    {
        private List<Empleado> _empleadosCache = new List<Empleado>();

        public PanelDeControl()
        {
            InitializeComponent();
            this.Loaded += (s, e) => CargarDatos();
        }

        private void CargarDatos()
        {
            _empleadosCache = DataService.Empleados;

            foreach (var emp in _empleadosCache)
            {
                var ultimaAsis = DataService.HistorialAsistencias
                    .Where(a => a.IdEmpleado == emp.Id)
                    .OrderByDescending(a => a.FechaHora)
                    .FirstOrDefault();

                if (ultimaAsis != null)
                {
                    emp.UltimoReporteHora = $"{ultimaAsis.FechaHora:HH:mm} hrs";
                    emp.UltimoReporteStatus = ultimaAsis.DentroDeRango ? "DENTRO DE RANGO" : "FUERA DE RANGO";
                    emp.UltimoReporteUbicacion = ultimaAsis.Incidencias;
                    emp.ColorStatus = ultimaAsis.DentroDeRango ? "#2ECC71" : "#E74C3C";
                }
                else
                {
                    emp.UltimoReporteHora = "SIN REPORTE";
                    emp.UltimoReporteStatus = "PENDIENTE";
                    emp.UltimoReporteUbicacion = "Esperando señal de n8n...";
                    emp.ColorStatus = "#4B5563";
                }
            }

            FiltrarYMostrar();
        }
        
        private void DgEmpleados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Tony: Verificamos que realmente se haya seleccionado un ítem para evitar el error de rango
                if (dgEmpleados.SelectedItem is Empleado seleccionado)
                {
                    if (this.NavigationService != null)
                    {
                        this.NavigationService.Navigate(new CalendarioEmpleado(seleccionado));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en Doble Clic: " + ex.Message);
            }
        }
        private void FiltrarYMostrar()
        {
            if (txtBusqueda == null || dgEmpleados == null) return;
            string filtro = txtBusqueda.Text.ToLower().Trim();

            var listaFiltrada = _empleadosCache.Where(emp =>
                emp.Nombre.ToLower().Contains(filtro) ||
                (emp.Matricula != null && emp.Matricula.ToLower().Contains(filtro))
            ).ToList();

            dgEmpleados.ItemsSource = null;
            dgEmpleados.ItemsSource = listaFiltrada;

            if (lblTotal != null) lblTotal.Text = $"AGENTES REGISTRADOS: {listaFiltrada.Count}";
            if (lblStatus != null) lblStatus.Text = $"SESIÓN: {SesionActual.Usuario?.Nombre.ToUpper()} | ROL: {(SesionActual.Usuario.EsSuperAdmin ? "SUPERADMIN" : "ADMIN")}";
        }

        // Navegación mediante doble clic en la fila
        private void BtnCalendario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Empleado seleccionado)
            {
                Dispatcher.BeginInvoke(new Action(() => {
                    if (this.NavigationService != null)
                    {
                        this.NavigationService.Navigate(new CalendarioEmpleado(seleccionado));
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e) => FiltrarYMostrar();

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            RegistroEmpleado win = new RegistroEmpleado();
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true) CargarDatos();
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado seleccionado)
            {
                if (seleccionado.EsSuperAdmin)
                {
                    MessageBox.Show("El Administrador Principal no puede ser eliminado por seguridad.", "Acción Denegada");
                    return;
                }

                string pass = Interaction.InputBox($"Confirme con su contraseña para eliminar a ({seleccionado.Nombre}):", "Seguridad de Datos", "");

                if (string.IsNullOrEmpty(pass)) return;

                if (pass == SesionActual.Usuario.Password)
                {
                    DataService.EliminarEmpleado(seleccionado.Id);
                    CargarDatos();
                }
                else { MessageBox.Show("Contraseña de administrador incorrecta.", "Error"); }
            }
        }

        private void BtnRoles_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado seleccionado)
            {
                if (seleccionado.EsSuperAdmin) return;

                string pass = Interaction.InputBox($"Autorice el cambio de rango para ({seleccionado.Nombre}):", "Gestión de Permisos", "");

                if (string.IsNullOrEmpty(pass)) return;

                if (pass == SesionActual.Usuario.Password)
                {
                    seleccionado.EsAdmin = !seleccionado.EsAdmin;
                    DataService.GuardarTodo();
                    CargarDatos();
                }
                else { MessageBox.Show("Autorización denegada."); }
            }
        }
    }
}