using ClosedXML.Excel;
using Microsoft.Win32;
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
    public partial class Turnos : Page
    {
        private DispatcherTimer _timer;
        private DateTime _fechaInicio;
        private DateTime _fechaFin;
        private ICollectionView _vistaSemanal;
        private ICollectionView _vistaPlana;
        private string _modoVistaActual = "SEMANAL";
        private bool _isLoaded = false;
        private bool _isCargando = false;

        public Turnos()
        {
            InitializeComponent();
            IniciarReloj();

            this.Loaded += (s, e) =>
            {
                if (_isLoaded) return;
                LoadEmployees();
                dpMaestro.SelectedDate = DateTime.Today;
                _isLoaded = true;
            };
            this.Unloaded += (s, e) => { if (_timer != null) _timer.Stop(); };

            // Buscador real que filtra la lista al escribir en el ComboBox
            cbEmpleados.KeyUp += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Down || e.Key == System.Windows.Input.Key.Up) return;

                cbEmpleados.IsDropDownOpen = true;
                var view = CollectionViewSource.GetDefaultView(cbEmpleados.ItemsSource);
                if (view != null)
                {
                    view.Filter = item =>
                    {
                        if (string.IsNullOrWhiteSpace(cbEmpleados.Text)) return true;
                        var emp = (EmpleadoCombo)item;
                        return emp.id_empleado == -1 || emp.nombre_completo.Contains(cbEmpleados.Text.ToUpper());
                    };
                    view.Refresh();
                }
            };
        }

        private void CbAccesosRapidos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpMaestro == null || cbAccesosRapidos == null || cbAccesosRapidos.SelectedItem == null) return;

            if (cbAccesosRapidos.SelectedItem is ComboBoxItem selectedItem)
            {
                string opcion = selectedItem.Tag?.ToString() ?? "";
                DateTime hoy = DateTime.Today;

                dpMaestro.SelectedDateChanged -= DpMaestro_SelectedDateChanged;

                switch (opcion)
                {
                    case "Hoy":
                        dpMaestro.SelectedDate = hoy;
                        break;
                    case "Semana":
                        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
                        dpMaestro.SelectedDate = hoy.AddDays(-1 * diff);
                        break;
                }

                dpMaestro.SelectedDateChanged += DpMaestro_SelectedDateChanged;

                CalcularFechas();
                cbAccesosRapidos.SelectedIndex = -1;
            }
        }

        private void IniciarReloj()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => lblReloj.Text = $"| {DateTime.Now:dd 'de' MMMM | hh:mm:ss tt}".ToUpper();
            _timer.Start();
        }

        private void LoadEmployees()
        {
            try
            {
                DataService.CargarEmpleados();

                var listaConTodos = new List<EmpleadoCombo> {
                    new EmpleadoCombo { id_empleado = -1, nombre_completo = "-- TODOS LOS EMPLEADOS --" }
                };

                var empleadosOrdenados = DataService.Empleados
                    .OrderBy(x => x.nombre_completo)
                    .Select(e => new EmpleadoCombo { id_empleado = e.id_empleado, nombre_completo = e.nombre_completo.ToUpper() });

                listaConTodos.AddRange(empleadosOrdenados);

                cbEmpleados.ItemsSource = listaConTodos;
                cbEmpleados.SelectedIndex = 0;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void CalcularFechas()
        {
            if (dpMaestro == null || cbTipoPeriodo == null || lblRangoTexto == null) return;
            if (dpMaestro.SelectedDate == null || cbTipoPeriodo.SelectedItem == null) return;

            DateTime f = dpMaestro.SelectedDate.Value.Date;

            if (cbTipoPeriodo.SelectedItem is ComboBoxItem item)
            {
                _modoVistaActual = item.Content?.ToString() ?? "SEMANAL";
            }
            else return;

            switch (_modoVistaActual)
            {
                case "HOY":
                    _fechaInicio = f;
                    _fechaFin = f;
                    ContenedorSemanal.Visibility = Visibility.Hidden;
                    ContenedorPlano.Visibility = Visibility.Visible;
                    break;
                case "SEMANAL":
                    int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
                    _fechaInicio = f.AddDays(-1 * diff).Date;
                    _fechaFin = _fechaInicio.AddDays(6).Date;
                    ContenedorSemanal.Visibility = Visibility.Visible;
                    ContenedorPlano.Visibility = Visibility.Hidden;
                    break;
            }

            lblRangoTexto.Text = $"DEL {_fechaInicio:dd MMM} AL {_fechaFin:dd MMM}".ToUpper();
            ApplyFilter();
        }

        private void CbTipoPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e) => CalcularFechas();
        private void DpMaestro_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => CalcularFechas();

        private void BtnAtras_Click(object sender, RoutedEventArgs e)
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime d = dpMaestro.SelectedDate.Value;

            if (_modoVistaActual == "HOY") dpMaestro.SelectedDate = d.AddDays(-1);
            else if (_modoVistaActual == "SEMANAL") dpMaestro.SelectedDate = d.AddDays(-7);
        }

        private void BtnAdelante_Click(object sender, RoutedEventArgs e)
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime d = dpMaestro.SelectedDate.Value;

            if (_modoVistaActual == "HOY") dpMaestro.SelectedDate = d.AddDays(1);
            else if (_modoVistaActual == "SEMANAL") dpMaestro.SelectedDate = d.AddDays(7);
        }

        private async void ApplyFilter()
        {
            if (dpMaestro == null || cbEmpleados == null || dgAsignacionesSemana == null) return;
            if (dpMaestro.SelectedDate == null) return;

            if (_isCargando) return;
            _isCargando = true;

            dgAsignacionesSemana.ItemsSource = null;
            dgAsignacionesPlana.ItemsSource = null;

            int idEmpleadoFiltro = -1;
            if (cbEmpleados.SelectedValue != null)
            {
                int.TryParse(cbEmpleados.SelectedValue.ToString(), out idEmpleadoFiltro);
            }

            DateTime inicio = _fechaInicio;
            DateTime fin = _fechaFin;
            string modo = _modoVistaActual;

            try
            {
                await Task.Run(() =>
                {
                    DataService.CargarAsignaciones();
                    DataService.CargarEmpleados();
                    DataService.CargarUbicaciones(); 

                    var asignaciones = DataService.Asignaciones
                        .Where(x => x.fecha.Date >= inicio && x.fecha.Date <= fin)
                        .ToList();

                    if (idEmpleadoFiltro != -1)
                    {
                        asignaciones = asignaciones.Where(x => x.id_empleado == idEmpleadoFiltro).ToList();
                    }

                    if (modo == "SEMANAL")
                    {
                        TablaReportes(asignaciones);
                    }
                    else
                    {
                        var resultadoPlano = new List<FilaVistaPlana>();

                        foreach (var a in asignaciones.OrderBy(x => x.fecha))
                        {
                            var emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == a.id_empleado);
                            var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == a.id_ubicacion);

                            resultadoPlano.Add(new FilaVistaPlana
                            {
                                IdEmpleado = a.id_empleado,
                                Fecha = a.fecha.ToString("dd/MM/yyyy"),
                                NombreEmpleado = emp?.nombre_completo?.ToUpper() ?? "DESC.",
                                Ubicacion = ubi?.nombre_lugar?.ToUpper() ?? "SIN UBICACIÓN",
                                Turno = FormatearTurnoDiario(a),
                                Estatus = FormatearEstatus(a)
                            });
                        }
                        Dispatcher.Invoke(() =>
                        {
                            dgAsignacionesPlana.ItemsSource = resultadoPlano;
                            _vistaPlana = CollectionViewSource.GetDefaultView(dgAsignacionesPlana.ItemsSource);
                            if (_vistaPlana != null) _vistaPlana.Filter = FiltroBusquedaPlana;
                        });
                    }
                });
            }
            finally
            {
                _isCargando = false;
            }
        }

        // TABLA NORMAL (INTERFAZ DE USUARIO XAML)
        private void TablaReportes(List<Asignacion> asignaciones)
        {
            var resultadoSemana = new List<FilaVistaSemanal>();
            var grupos = asignaciones.GroupBy(x => x.id_empleado);

            foreach (var g in grupos)
            {
                var emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == g.Key);
                if (emp == null) continue;

                resultadoSemana.Add(new FilaVistaSemanal
                {
                    IdEmpleado = g.Key,
                    NombreEmpleado = emp.nombre_completo?.ToUpper() ?? "SIN NOMBRE",
                    Lunes = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Monday),
                    Martes = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Tuesday),
                    Miercoles = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Wednesday),
                    Jueves = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Thursday),
                    Viernes = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Friday),
                    Sabado = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Saturday),
                    Domingo = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Sunday)
                });
            }

            Dispatcher.Invoke(() =>
            {
                dgAsignacionesSemana.ItemsSource = resultadoSemana.OrderBy(x => x.NombreEmpleado).ToList();
                _vistaSemanal = CollectionViewSource.GetDefaultView(dgAsignacionesSemana.ItemsSource);
                if (_vistaSemanal != null) _vistaSemanal.Filter = FiltroBusquedaSemanal;
            });
        }

        // FORMATO PARA LA UI Y LÓGICA DE ESTATUS
        private string GetTurnoTextoSemanal(List<Asignacion> turnos, DayOfWeek dia)
        {
            var t = turnos.FirstOrDefault(x => x.fecha.DayOfWeek == dia);
            if (t == null) return "-";

            var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == t.id_ubicacion);
            string lugar = ubi?.nombre_lugar?.ToUpper() ?? "SIN UBICACIÓN";
            string fecha = t.fecha.ToString("dd/MMM").ToUpper();
            string turno = FormatearTurnoDiario(t);

            // Llamada directa sin replaces
            string estatus = FormatearEstatus(t);

            return $"{fecha}\n{turno}\n📍 {lugar}\n{estatus}";
        }

        private string FormatearTurnoDiario(Asignacion t)
        {
            string estatusNorm = t.estatus?.Trim().ToUpper() ?? "";
            string descNorm = t.descripcion_del_turno?.Trim().ToUpper() ?? "";

            if (estatusNorm == "VACACIONES" || descNorm == "VACACIONES") return "VACACIONES";
            if (estatusNorm == "DÍA LIBRE" || estatusNorm == "DESCANSO" || descNorm.Contains("LIBRE") || descNorm.Contains("DESC")) return "DESCANSO";

            if (t.hora_inicio == TimeSpan.Zero && t.hora_fin == TimeSpan.Zero) return "24 HORAS";

            DateTime fIni = DateTime.Today.Add(t.hora_inicio);
            DateTime fFin = DateTime.Today.Add(t.hora_fin);
            return $"{fIni:hh:mm tt} - {fFin:hh:mm tt}";
        }

        private string FormatearEstatus(Asignacion t)
        {
            if (t == null) return "-";

            string estatusDB = t.estatus?.Trim().ToUpper() ?? "";
            string descNorm = t.descripcion_del_turno?.Trim().ToUpper() ?? "";

            if (estatusDB == "VACACIONES" || descNorm == "VACACIONES") return "Vacaciones";
            if (estatusDB == "DÍA LIBRE" || estatusDB == "DESCANSO" || descNorm.Contains("LIBRE") || descNorm.Contains("DESC")) return "Descanso";

            DateTime ahora = DateTime.Now;
            DateTime inicioAsignacion = t.fecha.Date.Add(t.hora_inicio);
            DateTime finAsignacion = t.fecha.Date.Add(t.hora_fin);

            // 1. Lógica App: Salida sin marcar (No mandó salida y ya terminó el turno)
            if ((estatusDB == "ASISTENCIA EN CURSO" || estatusDB == "ASISTENCIA COMPLETADA") && ahora > finAsignacion)
            {
                return "Salida sin marcar";
            }

            // 2. Lógica App: Estados previos a la asistencia
            if (string.IsNullOrEmpty(estatusDB) || estatusDB == "PROGRAMADA" || estatusDB == "PENDIENTE")
            {
                if (ahora < inicioAsignacion)
                {
                    return "Programada";
                }
                else if (ahora >= inicioAsignacion && ahora <= inicioAsignacion.AddMinutes(30)) // Margen de 30 minutos ajustable
                {
                    return "Pendiente de asistencia";
                }
                else if (ahora > inicioAsignacion.AddMinutes(30))
                {
                    return "No se marco asistencia";
                }
            }

            // 3. Lógica n8n: Respetar los estados exactos que inyecta el webhook
            if (!string.IsNullOrEmpty(t.estatus))
            {
                // Capitaliza solo la primera letra (ej. "Asistencia completada")
                if (t.estatus.Length > 1)
                    return char.ToUpper(t.estatus[0]) + t.estatus.Substring(1).ToLower();

                return t.estatus;
            }

            return "Desconocido";
        }
        private void CbEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            cbEmpleados.SelectionChanged -= CbEmpleados_SelectionChanged;
            cbTipoPeriodo.SelectionChanged -= CbTipoPeriodo_SelectionChanged;
            dpMaestro.SelectedDateChanged -= DpMaestro_SelectedDateChanged;

            cbEmpleados.SelectedIndex = 0;
            cbTipoPeriodo.SelectedIndex = 1;
            dpMaestro.SelectedDate = DateTime.Today;

            if (TxtBusqueda != null) TxtBusqueda.Text = string.Empty;

            var view = CollectionViewSource.GetDefaultView(cbEmpleados.ItemsSource);
            if (view != null) { view.Filter = null; view.Refresh(); }

            cbEmpleados.SelectionChanged += CbEmpleados_SelectionChanged;
            cbTipoPeriodo.SelectionChanged += CbTipoPeriodo_SelectionChanged;
            dpMaestro.SelectedDateChanged += DpMaestro_SelectedDateChanged;

            CalcularFechas();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportacionReportes();
        }

        // EXPORTACIÓN A EXCEL (ESTILO MATRIZ / SECORVI)
        private void ExportacionReportes()
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime f = dpMaestro.SelectedDate.Value;

            // Ajustamos el inicio de la semana (Lunes a Domingo)
            int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime inicioSemana = f.AddDays(-1 * diff).Date;
            DateTime finSemana = inicioSemana.AddDays(6).Date;

            // 1. CARGA DE DATOS DESDE EL SERVICIO
            DataService.CargarAsignaciones();
            DataService.CargarAsistencias(); 
            DataService.CargarEmpleados();

            // Filtramos las asignaciones del rango de fechas
            var asignacionesDelPeriodo = DataService.Asignaciones
                .Where(x => x.fecha >= inicioSemana && x.fecha <= finSemana)
                .ToList();

            if (!asignacionesDelPeriodo.Any())
            {
                MessageBox.Show("No hay datos de asignaciones en esta semana.", "Aviso");
                return;
            }

            try
            {
                var save = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"REPORTE_SECORVI_{inicioSemana:yyyyMMdd}.xlsx" };
                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Reporte de Asistencias");
                        int filaActual = 1;

                        // 1. CABECERAS (Basado en tu requerimiento de reporte plano)
                        ws.Cell(filaActual, 1).Value = "Nombre del empleado";
                        ws.Cell(filaActual, 2).Value = "Día";
                        ws.Cell(filaActual, 3).Value = "Turno";
                        ws.Cell(filaActual, 4).Value = "Lugar (Registro GPS)";
                        ws.Cell(filaActual, 5).Value = "Estatus";

                        // Estilo para cabeceras
                        var rangoCabeceras = ws.Range(filaActual, 1, filaActual, 5);
                        rangoCabeceras.Style.Font.Bold = true;
                        rangoCabeceras.Style.Fill.BackgroundColor = XLColor.FromHtml("#161920");
                        rangoCabeceras.Style.Font.FontColor = XLColor.White;
                        rangoCabeceras.SetAutoFilter();

                        ws.SheetView.FreezeRows(1);
                        filaActual++;

                        // 2. PREPARAR DATOS CRUZADOS (Asignación + Empleado + Transacción de Asistencia)
                        var listadoParaExcel = asignacionesDelPeriodo
                            .Select(asig => new
                            {
                                Asignacion = asig,
                                Emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == asig.id_empleado),
                                // Buscamos la transacción en la tabla asistencias usando el ID de asignación
                                Transaccion = DataService.Asistencias.FirstOrDefault(a => a.id_asignacion == asig.id_asignacion)
                            })
                            .OrderBy(x => x.Emp?.nombre_completo)
                            .ThenBy(x => x.Asignacion.fecha)
                            .ToList();

                        // 3. LLENADO DE LA LISTA
                        foreach (var item in listadoParaExcel)
                        {
                            ws.Cell(filaActual, 1).Value = item.Emp?.nombre_completo?.ToUpper() ?? "N/A";
                            ws.Cell(filaActual, 2).Value = item.Asignacion.fecha.ToString("dd/MM/yyyy");
                            ws.Cell(filaActual, 3).Value = FormatearTurnoDiario(item.Asignacion);

                            // LUGAR: Calculamos el ID de la ubicación basado en la asistencia real (si la hay) o la programada
                            int idUbiCalculada = item.Transaccion != null && item.Transaccion.id_ubicacion != 0
                                ? item.Transaccion.id_ubicacion
                                : item.Asignacion.id_ubicacion;

                            // Buscamos el nombre de ese lugar en la lista de ubicaciones cargadas
                            var ubiCalculada = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == idUbiCalculada);

                            ws.Cell(filaActual, 4).Value = ubiCalculada?.nombre_lugar?.ToUpper() ?? "SIN UBICACIÓN";

                            // ESTATUS: Usa la lógica centralizada
                            ws.Cell(filaActual, 5).Value = FormatearEstatus(item.Asignacion);

                            // Estilo de bordes
                            var filaRango = ws.Range(filaActual, 1, filaActual, 5);
                            filaRango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            filaRango.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                            filaActual++;
                        }

                        // 4. AJUSTE FINAL DE FORMATO
                        ws.Columns().AdjustToContents();
                        ws.Column(1).Width = 40; // Nombre
                        ws.Column(4).Width = 35; // Lugar/GPS
                        ws.Column(5).Width = 25; // Estatus

                        wb.SaveAs(save.FileName);
                        MessageBox.Show("Reporte transaccional generado con éxito.", "SECORVI System", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico al generar Excel: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // NAVEGADOR INTELIGENTE (FILTRO EN VIVO)
        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaSemanal?.Refresh();
            _vistaPlana?.Refresh();
        }

        private bool FiltroBusquedaSemanal(object item)
        {
            if (string.IsNullOrWhiteSpace(TxtBusqueda?.Text)) return true;

            var fila = item as FilaVistaSemanal;
            return fila != null && fila.NombreEmpleado.Contains(TxtBusqueda.Text.ToUpper());
        }

        private bool FiltroBusquedaPlana(object item)
        {
            if (string.IsNullOrWhiteSpace(TxtBusqueda?.Text)) return true;

            var fila = item as FilaVistaPlana;
            return fila != null && fila.NombreEmpleado.Contains(TxtBusqueda.Text.ToUpper());
        }

        // ==========================================
        // CLASES DE MODELO PARA LA VISTA
        // ==========================================
        public class EmpleadoCombo
        {
            public int id_empleado { get; set; }
            public string nombre_completo { get; set; }
        }

        public class FilaVistaPlana
        {
            public int IdEmpleado { get; set; }
            public string Fecha { get; set; }
            public string NombreEmpleado { get; set; }
            public string Ubicacion { get; set; }
            public string Turno { get; set; }
            public string Estatus { get; set; }
        }

        public class FilaVistaSemanal
        {
            public int IdEmpleado { get; set; }
            public string NombreEmpleado { get; set; }
            public string Lunes { get; set; }
            public string Martes { get; set; }
            public string Miercoles { get; set; }
            public string Jueves { get; set; }
            public string Viernes { get; set; }
            public string Sabado { get; set; }
            public string Domingo { get; set; }
        }
    }
}