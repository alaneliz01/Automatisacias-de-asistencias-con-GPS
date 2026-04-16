using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Secorvi
{
    public partial class PanelDeControl : Page
    {
        public PanelDeControl()
        {
            InitializeComponent();
            this.Loaded += (s, e) => CargarDatosDesdeDB();
        }

        private void CargarDatosDesdeDB()
        {
            try
            {
                DataService.ActualizarTodo();
                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = DataService.Empleados;

                ActualizarContador(DataService.Empleados.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR AL REFRESCAR PANEL: " + ex.Message, "DB ERROR");
            }
        }

        private void ActualizarContador(int total)
        {
            if (lblTotal != null)
                lblTotal.Text = $"AGENTES ACTIVOS: {total}";
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBusqueda.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(filtro))
            {
                dgEmpleados.ItemsSource = DataService.Empleados;
                ActualizarContador(DataService.Empleados.Count);
                return;
            }

            // Filtro dinámico sobre la lista en memoria
            var filtrados = DataService.Empleados
                .Where(x =>
                    (x.Nombre?.ToLower().Contains(filtro) ?? false) ||
                    (x.Apellido?.ToLower().Contains(filtro) ?? false) ||
                    (x.Matricula?.ToLower().Contains(filtro) ?? false) ||
                    (x.Usuario?.ToLower().Contains(filtro) ?? false)
                ).ToList();

            dgEmpleados.ItemsSource = filtrados;
            ActualizarContador(filtrados.Count);
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            RegistroEmpleado ventanaRegistro = new RegistroEmpleado();
            ventanaRegistro.Owner = Window.GetWindow(this);
            if (ventanaRegistro.ShowDialog() == true)
            {
                CargarDatosDesdeDB(); 
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                // Seguridad: No auto-eliminación
                if (SesionActual.Usuario != null && SesionActual.Usuario.Id == emp.Id)
                {
                    MessageBox.Show("PROTOCOL DENIED: No puede desactivar su propio perfil administrativo.", "SECURITY ALERT");
                    return;
                }

                var confirm = MessageBox.Show(
                    $"¿CONFIRMA DESACTIVACIÓN DEL AGENTE?\n\nNombre: {emp.Nombre} {emp.Apellido}\nMatrícula: {emp.Matricula}",
                    "CONFIRMACIÓN DE BAJA",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Llama al DataService que ya tiene el UPDATE SQL
                        DataService.EliminarEmpleado(emp.Id);

                        // Refrescamos la vista
                        CargarDatosDesdeDB();

                        MessageBox.Show("AGENTE DESACTIVADO CORRECTAMENTE.", "LOG: ÉXITO");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("FALLO EN OPERACIÓN: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un agente del roster.");
            }
        }

        private void BtnRoles_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                // Invertimos el rol actual
                bool nuevoEstado = !emp.EsAdmin;
                string rango = nuevoEstado ? "ADMINISTRADOR" : "AGENTE OPERATIVO";

                var result = MessageBox.Show($"¿Cambiar rango de {emp.Nombre} a {rango}?", "GESTIÓN DE ROLES", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        DataService.CambiarPermisos(emp.Id, nuevoEstado);
                        CargarDatosDesdeDB();
                        MessageBox.Show("RANGO ACTUALIZADO.", "SISTEMA");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private void BtnCalendario_Click(object sender, RoutedEventArgs e) => AbrirCalendarioSeleccionado();
        private void DgEmpleados_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => AbrirCalendarioSeleccionado();

        private void AbrirCalendarioSeleccionado()
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                NavigationService?.Navigate(new CalendarioEmpleado(emp));
            }
            else
            {
                MessageBox.Show("Seleccione un agente para desplegar calendario.");
            }
        }
    }
}