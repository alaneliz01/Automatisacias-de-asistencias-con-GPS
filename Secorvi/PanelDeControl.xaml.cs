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
//fin de todo
namespace Secorvi
{
    public partial class PanelDeControl : Page
    {
        private DispatcherTimer _autoRefreshTimer;
        private ICollectionView _empleadosView;

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

        private string EvaluarEstadoApp(Asignacion t)
        {
            string estatusDB = t.estatus?.Trim().ToUpper() ?? "";
            DateTime ahora = DateTime.Now;
            DateTime inicioAsignacion = t.fecha.Date.Add(t.hora_inicio);
            DateTime finAsignacion = t.fecha.Date.Add(t.hora_fin);

            if (t.hora_fin < t.hora_inicio)
            {
                finAsignacion = finAsignacion.AddDays(1);
            }

            if (string.IsNullOrEmpty(estatusDB) || estatusDB == "PROGRAMADA" || estatusDB == "PROGRAMADO" || estatusDB == "PENDIENTE")
            {
                if (ahora < inicioAsignacion)
                    return "Programada";

                if (ahora >= inicioAsignacion && ahora < finAsignacion)
                    return "Pendiente de asistencia";

                return "No se marco asistencia";
            }

            if (estatusDB == "ACTIVO" || estatusDB == "ENTRADA" || estatusDB == "ASISTENCIA EN CURSO")
            {
                if (estatusDB == "ACTIVO" && ahora < inicioAsignacion)
                    return "Programada";

                if (ahora > finAsignacion)
                    return "Salida sin marcar";

                return "Asistencia en curso";
            }

            // CÓDIGO CORREGIDO: Se restaura la validación de salida temprana
            if (estatusDB == "COMPLETADO" || estatusDB == "ASISTIÓ" || estatusDB == "ASISTENCIA COMPLETADA" || estatusDB == "SALIDA" || estatusDB == "SALIDA TEMPRANA")
            {
                // Si la base de datos ya dice salida temprana o si la hora actual es menor al fin del turno
                if (estatusDB == "SALIDA TEMPRANA" || ahora < finAsignacion)
                {
                    return "Salida temprana";
                }

                return "Asistencia completada";
            }

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
                await Task.Run(() =>
                {
                    DataService.ActualizarTodo();
                    DataService.CargarAsignaciones();
                });

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

                        if (asignacionHoy.hora_fin < asignacionHoy.hora_inicio)
                        {
                            fFin = fFin.AddDays(1);
                        }

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

                _empleadosView = CollectionViewSource.GetDefaultView(DataService.Empleados);

                _empleadosView.Filter = (obj) =>
                {
                    if (obj is Empleado emp)
                    {
                        string filtro = txtBusqueda?.Text?.Trim().ToLower() ?? "";
                        if (string.IsNullOrEmpty(filtro)) return true;

                        return (emp.nombre_completo?.ToLower().Contains(filtro) ?? false) ||
                                emp.id_empleado.ToString().Contains(filtro) ||
                                (emp.telefono?.Contains(filtro) ?? false);
                    }
                    return false;
                };

                if (dgEmpleados != null)
                {
                    dgEmpleados.ItemsSource = null;
                    dgEmpleados.ItemsSource = _empleadosView;
                }

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

            if (ventanaRegistro.ShowDialog() == true)
            {
                CargarDatosDesdeDB();
                int idNuevo = ventanaRegistro.IdEmpleadoGenerado;
                var fechas = new List<DateTime> { DateTime.Now.Date };
                this.NavigationService?.Navigate(new Mapa(idNuevo, fechas));
            }
        }

        private void BtnAsignacion_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Empleado emp)
            {
                var fechas = new List<DateTime> { DateTime.Today };
                Mapa paginaMapa = new Mapa(emp.id_empleado, fechas);
                this.NavigationService?.Navigate(paginaMapa);
            }
        }

        private void BtnCalendario_Click(object sender, RoutedEventArgs e) => AbrirCalendarioSeleccionado();

        private void DgEmpleados_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => AbrirCalendarioSeleccionado();

        private void AbrirCalendarioSeleccionado()
        {
            if (dgEmpleados?.SelectedItem is Empleado emp)
            {
                this.NavigationService?.Navigate(new CalendarioEmpleado(emp));
            }
        }

        private void BtnTurnos_Click(object sender, RoutedEventArgs e)
        {
        }

        private void dgEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}