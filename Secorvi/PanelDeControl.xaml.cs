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
        private string EvaluarEstadoApp(Asignacion t)
        {
            string estatusDB = t.estatus?.Trim().ToUpper() ?? "";

            // Homologar estatus crudos al estándar de la aplicación
            if (estatusDB == "ACTIVO" || estatusDB == "ENTRADA") estatusDB = "ASISTENCIA EN CURSO";
            if (estatusDB == "COMPLETADO" || estatusDB == "ASISTIÓ") estatusDB = "ASISTENCIA COMPLETADA";

            DateTime ahora = DateTime.Now;
            DateTime inicioAsignacion = t.fecha.Date.Add(t.hora_inicio);
            DateTime finAsignacion = t.fecha.Date.Add(t.hora_fin);

            if ((estatusDB == "ASISTENCIA EN CURSO" || estatusDB == "ASISTENCIA COMPLETADA") && ahora > finAsignacion)
            {
                return "Salida sin marcar";
            }

            if (string.IsNullOrEmpty(estatusDB) || estatusDB == "PROGRAMADA" || estatusDB == "PENDIENTE")
            {
                if (ahora < inicioAsignacion)
                    return "Programada";

                if (ahora >= inicioAsignacion && ahora <= inicioAsignacion.AddMinutes(30))
                    return "Pendiente de asistencia";

                return "No se marco asistencia";
            }

            // Retornar estatusDB capitalizado en lugar de t.estatus crudo
            if (!string.IsNullOrEmpty(estatusDB))
            {
                if (estatusDB.Length > 1)
                    return char.ToUpper(estatusDB[0]) + estatusDB.Substring(1).ToLower();

                return estatusDB;
            }

            return "Desconocido";
        }
        private async void CargarDatosDesdeDB()
        {
            try
            {
                // 1. Refrescamos toda la información desde MySQL en segundo plano
                await Task.Run(() =>
                {
                    DataService.ActualizarTodo();
                    DataService.CargarAsignaciones(); // Aseguramos tener las asignaciones listas para cruzar
                });

                // 2. NUEVA INTEGRACIÓN: Aplicar la lógica de la app a los empleados
                DateTime hoy = DateTime.Today;
                foreach (var emp in DataService.Empleados)
                {
                    var asignacionHoy = DataService.Asignaciones
                        .FirstOrDefault(a => a.id_empleado == emp.id_empleado && a.fecha.Date == hoy);

                    if (asignacionHoy != null)
                    {
                        emp.estatus_asistencia = EvaluarEstadoApp(asignacionHoy);

                        DateTime fIni = DateTime.Today.Add(asignacionHoy.hora_inicio);
                        DateTime fFin = DateTime.Today.Add(asignacionHoy.hora_fin);

                        if (asignacionHoy.hora_inicio == TimeSpan.Zero && asignacionHoy.hora_fin == TimeSpan.Zero)
                            emp.info_turno = "24 HORAS";
                        else if (asignacionHoy.estatus?.Trim().ToUpper() == "DESCANSO" || asignacionHoy.estatus?.Trim().ToUpper() == "VACACIONES")
                            emp.info_turno = asignacionHoy.estatus.ToUpper();
                        else
                            emp.info_turno = $"{fIni:hh:mm tt} - {fFin:hh:mm tt}";
                    }
                    else
                    {
                        emp.estatus_asistencia = "Sin asignación hoy";
                        emp.info_turno = "-";
                    }
                }

                // 3. Obtenemos la vista de la colección
                _empleadosView = CollectionViewSource.GetDefaultView(DataService.Empleados);

                // 4. Configuramos el filtro
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

                // 5. Inyectamos a la tabla
                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = _empleadosView;

                // 6. Refrescamos la vista de colección
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