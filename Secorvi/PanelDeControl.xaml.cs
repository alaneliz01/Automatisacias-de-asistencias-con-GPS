using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
            // Validar primero si es un día de descanso o vacaciones
            string estatusAsignacion = asignacion.estatus?.Trim().ToUpper() ?? "";

            if (estatusAsignacion == "VACACIONES")
                return "Vacaciones";

            if (estatusAsignacion == "DESCANSO" || estatusAsignacion == "DESCANSOS")
                return "Descansos";

            DateTime ahora = DateTime.Now;
            DateTime inicioProgr = asignacion.fecha.Date.Add(asignacion.hora_inicio);
            DateTime finProgr = asignacion.fecha.Date.Add(asignacion.hora_fin);

            if (asignacion.hora_fin < asignacion.hora_inicio)
                finProgr = finProgr.AddDays(1);

            if (asistencia == null)
            {
                if (ahora < inicioProgr) return "Programada";

                // Si ya pasaron 30 minutos o más de la hora de inicio (Límite para tomar asistencia)
                if (ahora >= inicioProgr.AddMinutes(30)) return "No se marco la entrada";

                if (ahora < finProgr) return "Pendiente de marcar entrada";

                return "No se marco la entrada";
            }

            string estatus = asistencia.estatus?.Trim().ToUpper() ?? "";

            // Validación para "No se marcó la salida" (Pasó la hora límite del turno y no completó)
            // Usamos 30 min de tolerancia, ajusta el AddMinutes según tu regla de negocio
            if (estatus != "SALIDA COMPLETADA" && ahora > finProgr.AddMinutes(30))
            {
                return "No se marco la salida";
            }

            return estatus switch
            {
                "ASISTENCIA EN CURSO" => !asistencia.fecha_fin.HasValue
                    ? "Marcando entrada"    // Solo mandó número, falta ubicación
                    : "Entrada registrada", // Mandó número y ubicación

                "ASISTENCIA COMPLETADA" => "Entrada registrada",

                "SALIDA COMPLETADA" => EvaluarSalidaCompletada(asistencia, finProgr),

                _ => estatus != "" ? estatus : "Desconocido"
            };
        }

        private string EvaluarSalidaCompletada(Asistencia asistencia, DateTime finProgr)
        {
            if (!asistencia.hora_fin.HasValue)
                return "Asistencia completa";

            DateTime salidaReal = asistencia.fecha_fin.HasValue
                ? asistencia.fecha_fin.Value.Date.Add(asistencia.hora_fin.Value)
                : DateTime.MinValue;

            if (salidaReal < finProgr)
            {
                TimeSpan diferencia = finProgr - salidaReal;
                int horas = diferencia.Hours;
                int minutos = diferencia.Minutes;

                string tiempoFormateado = horas > 0
                    ? (minutos > 0 ? $"{horas} h {minutos} min" : $"{horas} h")
                    : $"{minutos} min";

                // Se mantiene el mensaje de salida temprana con el tiempo exacto
                return $"Salida temprana ({tiempoFormateado} antes)";
            }

            // Si salió a la hora o después, muestra el mensaje de la tabla
            return "Asistencia completa";
        }

        private (string progEntrada, string progSalida, string realEntrada, string realSalida) FormatearInfoTurno(Asignacion asignacion, Asistencia asistencia)
        {
            string progEnt = $"Entrada: {asignacion.hora_inicio:hh\\:mm}";
            string progSal = $"Salida:  {asignacion.hora_fin:hh\\:mm}";

            if (asignacion.hora_inicio == TimeSpan.Zero && asignacion.hora_fin == TimeSpan.Zero)
                return ("24 HORAS", "", "--:--", "--:--");

            if (asignacion.estatus?.Trim().ToUpper() == "DESCANSO" ||
                asignacion.estatus?.Trim().ToUpper() == "DESCANSOS" ||
                asignacion.estatus?.Trim().ToUpper() == "VACACIONES")
            {
                // Capitalizamos la primera letra para presentación
                string estatusFormateado = char.ToUpper(asignacion.estatus[0]) + asignacion.estatus.Substring(1).ToLower();
                return (estatusFormateado, "", "", "");
            }

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

    // ---------------------------------------------------------------------
    // CONVERTERS MODIFICADOS
    // ---------------------------------------------------------------------
    public class EstatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string estatus = value as string;
            var converter = new BrushConverter();

            if (!string.IsNullOrEmpty(estatus))
            {
                if (estatus.StartsWith("Salida temprana"))
                {
                    return (Brush)converter.ConvertFromString("#2D240A");
                }

                if (estatus == "No se marco asistencia")
                {
                    // Fondo rojo oscuro
                    return (Brush)converter.ConvertFromString("#3E1414");
                }
            }

            return (Brush)converter.ConvertFromString("#1A1F26");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EstatusBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string estatus = value as string;
            var converter = new BrushConverter();

            if (!string.IsNullOrEmpty(estatus))
            {
                if (estatus.StartsWith("Salida temprana"))
                {
                    return (Brush)converter.ConvertFromString("#F1C40F");
                }

                if (estatus == "No se marco asistencia")
                {
                    // Borde rojo brillante
                    return (Brush)converter.ConvertFromString("#E74C3C");
                }
            }

            return (Brush)converter.ConvertFromString("#2D323E");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}