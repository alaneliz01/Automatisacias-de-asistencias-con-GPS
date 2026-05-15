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

        private string EvaluarEstadoApp(Asignacion asignacion, Asistencia asistencia)
        {
            DateTime ahora = DateTime.Now;
            DateTime inicioProgr = asignacion.fecha.Date.Add(asignacion.hora_inicio);
            DateTime finProgr = asignacion.fecha.Date.Add(asignacion.hora_fin);

            // Manejo de turnos nocturnos
            if (asignacion.hora_fin < asignacion.hora_inicio)
                finProgr = finProgr.AddDays(1);

            // ── 1. Sin registro de asistencia (asistencia es null en la DB) ──
            if (asistencia == null)
            {
                if (ahora < inicioProgr) return "Programada";
                if (ahora < finProgr) return "Pendiente de asistencia";
                return "No se marcó asistencia";
            }

            // ── 2.
            string estatus = asistencia.estatus?.Trim()?? "";

            return estatus.ToUpper() switch
            {
                "ASISTENCIA EN CURSO" => !asistencia.fecha_fin.HasValue 
                ? "Asistencia en curso, pendiente de mandar ubicacion" 
                : "Asistencia en curso",
                "ASISTENCIA COMPLETADA" => "Asistencia completada, pendiente de marcar salida",
                "SALIDA COMPLETADA" => "Salida completada",
                _                   => estatus != "" ? estatus: "Desconocido"
            };
        }

        // Función auxiliar para que el Excel se vea profesional (ej: "VACACIONES" -> "Vacaciones")
        private string CapitalizarTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return char.ToUpper(texto[0]) + texto.Substring(1).ToLower();
        }

        private string FormatearEstatus(string estatus)
        {
            if (string.IsNullOrEmpty(estatus)) return "Desconocido";
            return char.ToUpper(estatus[0]) + estatus.Substring(1).ToLower();
        }
        private (string progEntrada, string progSalida, string realEntrada, string realSalida)
    FormatearInfoTurno(Asignacion asignacion, Asistencia asistencia)
        {
            string progEnt = $"Entrada: {asignacion.hora_inicio:hh\\:mm}";
            string progSal = $"Salida:  {asignacion.hora_fin:hh\\:mm}";

            if (asignacion.hora_inicio == TimeSpan.Zero && asignacion.hora_fin == TimeSpan.Zero)
                return ("24 HORAS", "", "--:--", "--:--");

            if (asignacion.estatus?.Trim().ToUpper() == "DESCANSO" ||
                asignacion.estatus?.Trim().ToUpper() == "VACACIONES")
                return (asignacion.estatus.ToUpper(), "", "", "");

            if (asistencia == null)
                return (progEnt, progSal, "Entrada: --:--", "Salida:  --:--");

            string estatus = asistencia.estatus?.Trim().ToUpper() ?? "";

            string entradaReal = $"Entrada: {asistencia.hora_inicio:hh\\:mm}";

            if (estatus == "ASISTENCIA EN CURSO" || estatus == "ASISTENCIA COMPLETADA")
                return (progEnt, progSal, entradaReal, "Salida:  --:--");

            if (estatus == "SALIDA COMPLETADA" && asistencia.hora_fin.HasValue)
                return (progEnt, progSal, entradaReal, $"Salida:  {asistencia.hora_fin.Value:hh\\:mm}");

            return (progEnt, progSal, "Entrada: --:--", "Salida:  --:--");
        }

        private async void CargarDatosDesdeDB()
        {
            try
            {
                await Task.Run(() =>
                {
                    DataService.ActualizarTodo();
                    DataService.CargarAsignaciones();
                    DataService.CargarAsistencias();
                });

                DateTime hoy = DateTime.Today;
                foreach (var emp in DataService.Empleados)
                {
                    var asignacionHoy = DataService.Asignaciones
                    .FirstOrDefault(a => a.id_empleado == emp.id_empleado && a.fecha.Date == hoy)
                    ?? DataService.Asignaciones
                    .FirstOrDefault(a =>
                        a.id_empleado == emp.id_empleado &&
                        a.fecha.Date == hoy.AddDays(-1) &&
                        a.hora_fin < a.hora_inicio);

                    if (asignacionHoy != null)
                    {
                        var asistenciaHoy = DataService.Asistencias
                            .FirstOrDefault(a => a.id_asignacion == asignacionHoy.id_asignacion
                          && a.id_empleado == emp.id_empleado);

                        emp.estatus_asistencia = EvaluarEstadoApp(asignacionHoy, asistenciaHoy);
                        var (progEnt, progSal, realEnt, realSal) = FormatearInfoTurno(asignacionHoy, asistenciaHoy);
                        emp.info_prog_entrada = progEnt;
                        emp.info_prog_salida = progSal;
                        emp.info_turno_entrada = realEnt;
                        emp.info_turno_salida = realSal;
                    }
                    else
                    {
                        emp.estatus_asistencia = "Sin asignación hoy";
                        emp.info_turno_entrada = "-";
                        emp.info_turno_salida = "-";
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