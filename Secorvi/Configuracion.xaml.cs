using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel; // REQUERIDO PARA ICollectionView
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Secorvi
{
    public partial class Configuracion : Page
    {
        private Empleado? empleadoSeleccionado;
        private Ubicacion? ubicacionSeleccionada;

        // Variable para gestionar el filtrado en el DataGrid
        private ICollectionView? _empleadosView;
        private ICollectionView? _ubicacionesView;
        public Configuracion()
        {
            InitializeComponent();
            ConfigurarDropdownsHoras();
            CargarDatos();
        }

        private void CargarDatos()
        {
            DataService.CargarEmpleados();
            DataService.CargarUbicaciones();

            // Vista Personal
            _empleadosView = CollectionViewSource.GetDefaultView(DataService.Empleados);
            _empleadosView.Filter = FiltroEmpleados;
            dgEmpleados.ItemsSource = _empleadosView;

            // Vista Ubicaciones
            _ubicacionesView = CollectionViewSource.GetDefaultView(DataService.Ubicaciones);
            _ubicacionesView.Filter = FiltroUbicaciones; 
            dgUbicaciones.ItemsSource = _ubicacionesView;
            ActualizarContadorUI();
        }
        private bool FiltroUbicaciones(object obj)
        {
            if (obj is Ubicacion ubi)
            {
                if (string.IsNullOrWhiteSpace(txtBusquedaUbi?.Text)) return true;
                return ubi.nombre_lugar.ToLower().Contains(txtBusquedaUbi.Text.ToLower());
            }
            return false;
        }

        private void TxtBusquedaUbi_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ubicacionesView?.Refresh();
            ActualizarContadorUI();
        }

        // LÓGICA DE FILTRADO
        private bool FiltroEmpleados(object obj)
        {
            if (obj is Empleado emp)
            {
                // Si el cuadro de búsqueda no existe o está vacío, mostrar todos
                if (string.IsNullOrWhiteSpace(txtBusqueda?.Text)) return true;

                string filtro = txtBusqueda.Text.ToLower();
                return emp.nombre_completo.ToLower().Contains(filtro) ||
                       emp.usuario.ToLower().Contains(filtro) ||
                       emp.telefono.Contains(filtro);
            }
            return false;
        }

        private void ActualizarContadorUI()
        {
            if (_empleadosView != null)
                lblContador.Text = $"{_empleadosView.Cast<object>().Count()} AGENTES";

            // Contador para la pestaña de ubicaciones
            if (_ubicacionesView != null)
                lblContadorUbi.Text = $"{_ubicacionesView.Cast<object>().Count()} ZONAS";
        }
        // EL MÉTODO QUE SOLICITASTE
        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            _empleadosView?.Refresh();
            ActualizarContadorUI();
        }

        // ==========================================
        // LÓGICA DE EMPLEADOS Y SEGURIDAD
        // ==========================================
        private void dgEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado emp)
            {
                empleadoSeleccionado = emp;

                txtEmpNombre.Text = emp.nombre_completo;
                txtEmpTelefono.Text = emp.telefono;
                txtEmpUsuario.Text = emp.usuario;
                txtEmpPassword.Text = "";

                if (emp.id_rol == 1) cmbEmpRol.SelectedIndex = 0;
                else if (emp.id_rol == 2) cmbEmpRol.SelectedIndex = 1;
                else cmbEmpRol.SelectedIndex = 2;

                panelAutorizacion.Visibility = Visibility.Collapsed;
                txtAuthUsuario.Clear();
                txtAuthPassword.Clear();
            }
        }

        private void cmbEmpRol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (empleadoSeleccionado == null || cmbEmpRol.SelectedIndex == -1) return;

            int rolDeseado = cmbEmpRol.SelectedIndex + 1;

            if (rolDeseado < empleadoSeleccionado.id_rol && (rolDeseado == 1 || rolDeseado == 2))
            {
                panelAutorizacion.Visibility = Visibility.Visible;
                btnGuardarEmpleado.Content = "VERIFICAR Y GUARDAR";
                btnGuardarEmpleado.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));
            }
            else
            {
                panelAutorizacion.Visibility = Visibility.Collapsed;
                btnGuardarEmpleado.Content = "GUARDAR CAMBIOS";
                btnGuardarEmpleado.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
        }
        private void btnEliminarEmpleado_Click(object sender, RoutedEventArgs e)
        {
            if (empleadoSeleccionado == null)
            {
                MessageBox.Show("Por favor, selecciona un agente primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. EL BLOQUEO DE SEGURIDAD VA AQUÍ (Frontend)
            if (empleadoSeleccionado.id_empleado == 1) // Asumiendo que la propiedad se llama id_empleado
            {
                MessageBox.Show("PROTOCOLO DE SEGURIDAD: La cuenta del Director Principal no puede ser eliminada del sistema.", "OPERACIÓN DENEGADA", MessageBoxButton.OK, MessageBoxImage.Stop);
                return; // Cortamos la ejecución aquí. La ventana de confirmación nunca se abre.
            }

            // 2. Si pasa el filtro, abrimos tu ventana de confirmación
            var ventanaConfirmacion = new ConfirmarEliminacion(empleadoSeleccionado.nombre_completo);

            if (ventanaConfirmacion.ShowDialog() == true && ventanaConfirmacion.ResultadoValidacion)
            {
                try
                {
                    // 3. Ejecutar la eliminación real
                    // DataService.EliminarEmpleado(empleadoSeleccionado.id_empleado);

                    MessageBox.Show("Agente eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    // CargarDatos(); // Refrescar el DataGrid
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnGuardarEmpleado_Click(object sender, RoutedEventArgs e)
        {
            if (empleadoSeleccionado == null)
            {
                MessageBox.Show("Por favor, selecciona un agente de la tabla primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int rolDeseado = cmbEmpRol.SelectedIndex + 1;

            if (empleadoSeleccionado.id_empleado == 1 && rolDeseado > 1)
            {
                MessageBox.Show("PROTOCOLO DE SEGURIDAD: No está permitido degradar la cuenta del Director Principal (Rommel).", "OPERACIÓN DENEGADA", MessageBoxButton.OK, MessageBoxImage.Stop);
                cmbEmpRol.SelectedIndex = 0;
                return;
            }

            if (panelAutorizacion.Visibility == Visibility.Visible)
            {
                string authUser = txtAuthUsuario.Text;
                string authPass = txtAuthPassword.Password;

                if (string.IsNullOrWhiteSpace(authUser) || string.IsNullOrWhiteSpace(authPass))
                {
                    MessageBox.Show("Debe ingresar las credenciales de autorización para procesar este ascenso.", "FALTA FIRMA", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var adminAutorizador = DataService.Empleados.FirstOrDefault(emp =>
                    emp.usuario == authUser &&
                    emp.contrasena == authPass &&
                    emp.id_rol == 1);

                if (adminAutorizador == null)
                {
                    MessageBox.Show("FIRMA INVÁLIDA: Las credenciales ingresadas no pertenecen a un Super Admin autorizado o son incorrectas.", "BRECHA DE SEGURIDAD", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtAuthPassword.Clear();
                    return;
                }
            }

            empleadoSeleccionado.nombre_completo = txtEmpNombre.Text;
            empleadoSeleccionado.telefono = txtEmpTelefono.Text;
            empleadoSeleccionado.usuario = txtEmpUsuario.Text;
            empleadoSeleccionado.id_rol = rolDeseado;

            if (!string.IsNullOrWhiteSpace(txtEmpPassword.Text))
            {
                empleadoSeleccionado.contrasena = txtEmpPassword.Text;
            }

            DataService.ActualizarEmpleado(empleadoSeleccionado);

            MessageBox.Show("Datos del empleado actualizados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            txtAuthUsuario.Clear();
            txtAuthPassword.Clear();
            panelAutorizacion.Visibility = Visibility.Collapsed;
            CargarDatos();
        }

        // ==========================================
        // LÓGICA DE UBICACIONES Y HORARIOS
        // ==========================================
        private void ConfigurarDropdownsHoras()
        {
            var listaHoras = new List<string>();
            for (int i = 1; i <= 12; i++)
            {
                listaHoras.Add($"{i:D2}:00");
                listaHoras.Add($"{i:D2}:30");
            }
            cbUbiHoraInicio.ItemsSource = listaHoras;
            cbUbiHoraFin.ItemsSource = listaHoras;

            cbUbiHoraInicio.Text = "08:00";
            cbUbiAmPmInicio.SelectedIndex = 0;
            cbUbiHoraFin.Text = "04:00";
            cbUbiAmPmFin.SelectedIndex = 1;
        }

        private void dgUbicaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUbicaciones.SelectedItem is Ubicacion ubi)
            {
                ubicacionSeleccionada = ubi;
                txtUbiNombre.Text = ubi.nombre_lugar;

                if (ubi.hora_inicio_default.HasValue)
                    SetPickersFromTimeSpan(ubi.hora_inicio_default.Value, cbUbiHoraInicio, cbUbiAmPmInicio);

                if (ubi.hora_fin_default.HasValue)
                    SetPickersFromTimeSpan(ubi.hora_fin_default.Value, cbUbiHoraFin, cbUbiAmPmFin);
            }
        }

        private void btnGuardarUbicacion_Click(object sender, RoutedEventArgs e)
        {
            if (ubicacionSeleccionada == null)
            {
                MessageBox.Show("Por favor, selecciona una ubicación de la tabla primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ubicacionSeleccionada.nombre_lugar = txtUbiNombre.Text;
                ubicacionSeleccionada.hora_inicio_default = GetTimeSpanFromPickers(cbUbiHoraInicio, cbUbiAmPmInicio);
                ubicacionSeleccionada.hora_fin_default = GetTimeSpanFromPickers(cbUbiHoraFin, cbUbiAmPmFin);

                DataService.ActualizarUbicacion(ubicacionSeleccionada);

                MessageBox.Show("Ubicación y horario actualizados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEliminarUbicacion_Click(object sender, RoutedEventArgs e)
        {
            if (ubicacionSeleccionada == null)
            {
                MessageBox.Show("Por favor, selecciona una ubicación para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmacion = MessageBox.Show($"¿Estás seguro de eliminar la zona '{ubicacionSeleccionada.nombre_lugar}'?",
                                               "CONFIRMAR ELIMINACIÓN", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmacion == MessageBoxResult.Yes)
            {
                try
                {
                    DataService.EliminarUbicacion(ubicacionSeleccionada.id_ubicacion);
                    MessageBox.Show("Zona eliminada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    ubicacionSeleccionada = null;
                    txtUbiNombre.Clear();
                    CargarDatos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // HELPERS DE TIEMPO
        private TimeSpan GetTimeSpanFromPickers(ComboBox cbHora, ComboBox cbAmPm)
        {
            if (string.IsNullOrEmpty(cbHora.Text)) return TimeSpan.Zero;
            string[] partes = cbHora.Text.Split(':');
            int horas = int.Parse(partes[0]);
            int minutos = int.Parse(partes[1]);
            string amPm = (cbAmPm.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (amPm == "PM" && horas < 12) horas += 12;
            if (amPm == "AM" && horas == 12) horas = 0;

            return new TimeSpan(horas, minutos, 0);
        }

        private void SetPickersFromTimeSpan(TimeSpan ts, ComboBox cbHora, ComboBox cbAmPm)
        {
            int h = ts.Hours;
            string amPm = "AM";
            if (h >= 12) { amPm = "PM"; if (h > 12) h -= 12; }
            if (h == 0) h = 12;
            cbHora.SelectedItem = $"{h:D2}:{ts.Minutes:D2}";
            cbAmPm.SelectedIndex = (amPm == "PM") ? 1 : 0;
        }
    }
}