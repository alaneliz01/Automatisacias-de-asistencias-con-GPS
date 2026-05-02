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

        // Variables de control para evitar ejecuciones múltiples
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

                // Desactivamos temporalmente el evento del DatePicker para no disparar cálculos dobles
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

                    var asignaciones = DataService.Asignaciones
                        .Where(x => x.fecha.Date >= inicio && x.fecha.Date <= fin)
                        .ToList();

                    if (idEmpleadoFiltro != -1)
                    {
                        asignaciones = asignaciones.Where(x => x.id_empleado == idEmpleadoFiltro).ToList();
                    }

                    if (modo == "SEMANAL")
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
                    else
                    {
                        var resultadoPlano = new List<FilaVistaPlana>();
                        DataService.CargarUbicaciones();

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
                                Estatus = string.IsNullOrEmpty(a.estatus) ? "PENDIENTE" : a.estatus.ToUpper()
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

        // ==========================================
        // FORMATO PARA LA UI (TABLAS)
        // ==========================================
        private string GetTurnoTextoSemanal(List<Asignacion> turnos, DayOfWeek dia)
        {
            var t = turnos.FirstOrDefault(x => x.fecha.DayOfWeek == dia);
            if (t == null) return "-";
            return FormatearTurnoSemanal(t);
        }

        private string FormatearTurnoSemanal(Asignacion t)
        {
            string estatusNorm = t.estatus?.Trim().ToUpper() ?? "";
            string descNorm = t.descripcion_del_turno?.Trim().ToUpper() ?? "";

            if (estatusNorm == "VACACIONES" || descNorm == "VACACIONES") return "VACACIONES";
            if (estatusNorm == "DÍA LIBRE" || estatusNorm == "DESCANSO" || descNorm.Contains("LIBRE") || descNorm.Contains("DESC")) return "DESCANSO";

            string txtInfo = string.IsNullOrEmpty(descNorm) ? "ASIGNADO" : descNorm;
            if (t.hora_inicio == TimeSpan.Zero && t.hora_fin == TimeSpan.Zero) return $"{txtInfo}\n24 HORAS";

            DateTime fIni = DateTime.Today.Add(t.hora_inicio);
            DateTime fFin = DateTime.Today.Add(t.hora_fin);
            return $"{txtInfo}\n{fIni:hh:mm tt} - {fFin:hh:mm tt}";
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

        // ==========================================
        // LÓGICA DE EXTRACCIÓN DE ESTATUS
        // ==========================================
        private string GetSoloEstatus(List<Asignacion> turnos, DayOfWeek dia)
        {
            var t = turnos.FirstOrDefault(x => x.fecha.DayOfWeek == dia);
            return FormatearEstatus(t);
        }

        private string FormatearEstatus(Asignacion t)
        {
            if (t == null) return "-";

            string estatusNorm = t.estatus?.Trim().ToUpper() ?? "";
            string descNorm = t.descripcion_del_turno?.Trim().ToUpper() ?? "";

            if (estatusNorm == "VACACIONES" || descNorm == "VACACIONES") return "🏖️ VACACIONES";
            if (estatusNorm == "DÍA LIBRE" || estatusNorm == "DESCANSO" || descNorm.Contains("LIBRE") || descNorm.Contains("DESC")) return "💤 DESCANSO";

            return (estatusNorm == "COMPLETADO" || estatusNorm == "ASISTIÓ") ? "✅ ASISTIÓ" : (estatusNorm == "FALTA" ? "❌ FALTÓ" : "⏳ PENDIENTE");
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

        // ==========================================
        // EXPORTACIONES A EXCEL (NUEVO MENÚ DE 4 OPCIONES)
        // ==========================================
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = new ContextMenu();

            MenuItem miSemanal = new MenuItem { Header = "1. Reporte Semanal Básico" };
            miSemanal.Click += (s, ev) => ExportarReporteSemanal();
            cm.Items.Add(miSemanal);

            MenuItem miSemanalAsist = new MenuItem { Header = "2. Reporte Semanal + Asistencia" };
            miSemanalAsist.Click += (s, ev) => ExportarReporteSemanalConAsistencia();
            cm.Items.Add(miSemanalAsist);

            MenuItem miDiario = new MenuItem { Header = "3. Reporte Diario Básico" };
            miDiario.Click += (s, ev) => ExportarReporteDiario(false);
            cm.Items.Add(miDiario);

            MenuItem miDiarioAsist = new MenuItem { Header = "4. Reporte Diario + Asistencia" };
            miDiarioAsist.Click += (s, ev) => ExportarReporteDiario(true);
            cm.Items.Add(miDiarioAsist);

            Button btn = sender as Button;
            cm.PlacementTarget = btn;
            cm.IsOpen = true;
        }

        private void ExportarReporteSemanal()
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime f = dpMaestro.SelectedDate.Value;
            int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime inicioSemana = f.AddDays(-1 * diff).Date;
            DateTime finSemana = inicioSemana.AddDays(6).Date;

            DataService.CargarAsignaciones();
            var datos = DataService.Asignaciones.Where(x => x.fecha >= inicioSemana && x.fecha <= finSemana).ToList();
            if (!datos.Any()) { MessageBox.Show("No hay datos en esta semana.", "Aviso"); return; }

            try
            {
                var save = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"REPORTE_SEMANAL_{DateTime.Now:yyyyMMdd}.xlsx" };
                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Reporte Semanal");
                        string[] h = { "ID", "EMPLEADO", "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO" };

                        for (int i = 0; i < h.Length; i++)
                        {
                            ws.Cell(1, i + 1).Value = h[i];
                            ws.Cell(1, i + 1).Style.Font.Bold = true;
                            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFB300");
                            ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        int r = 2;
                        var grupos = datos.GroupBy(x => x.id_empleado);
                        foreach (var g in grupos)
                        {
                            var emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == g.Key);
                            ws.Cell(r, 1).Value = g.Key;
                            ws.Cell(r, 2).Value = emp != null ? emp.nombre_completo.ToUpper() : "";
                            ws.Cell(r, 3).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Monday);
                            ws.Cell(r, 4).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Tuesday);
                            ws.Cell(r, 5).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Wednesday);
                            ws.Cell(r, 6).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Thursday);
                            ws.Cell(r, 7).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Friday);
                            ws.Cell(r, 8).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Saturday);
                            ws.Cell(r, 9).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Sunday);

                            for (int col = 3; col <= 9; col++) { ws.Cell(r, col).Style.Alignment.WrapText = true; ws.Cell(r, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; }
                            r++;
                        }
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(save.FileName);
                        MessageBox.Show("¡Exportado correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); }
        }

        private void ExportarReporteSemanalConAsistencia()
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime f = dpMaestro.SelectedDate.Value;
            int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime inicioSemana = f.AddDays(-1 * diff).Date;
            DateTime finSemana = inicioSemana.AddDays(6).Date;

            DataService.CargarAsignaciones();
            var datos = DataService.Asignaciones.Where(x => x.fecha >= inicioSemana && x.fecha <= finSemana).ToList();
            if (!datos.Any()) { MessageBox.Show("No hay datos.", "Aviso"); return; }

            try
            {
                var save = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"SEMANAL_ASISTENCIA_{DateTime.Now:yyyyMMdd}.xlsx" };
                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Asistencia");

                        // SEPARADO EN COLUMNAS PARA MEJOR ANÁLISIS
                        string[] h = { "ID", "EMPLEADO",
                                       "LUNES (TURNO)", "LUNES (ASISTENCIA)",
                                       "MARTES (TURNO)", "MARTES (ASISTENCIA)",
                                       "MIERCOLES (TURNO)", "MIERCOLES (ASISTENCIA)",
                                       "JUEVES (TURNO)", "JUEVES (ASISTENCIA)",
                                       "VIERNES (TURNO)", "VIERNES (ASISTENCIA)",
                                       "SABADO (TURNO)", "SABADO (ASISTENCIA)",
                                       "DOMINGO (TURNO)", "DOMINGO (ASISTENCIA)" };

                        for (int i = 0; i < h.Length; i++)
                        {
                            ws.Cell(1, i + 1).Value = h[i];
                            ws.Cell(1, i + 1).Style.Font.Bold = true;
                            ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Color verde para Datos Base
                            if (i < 2) ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4CAF50");
                            // Color gris claro para Turnos
                            else if (i % 2 == 0) ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E0E0");
                            // Color verde claro para Asistencia
                            else ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
                        }

                        int r = 2;
                        var grupos = datos.GroupBy(x => x.id_empleado);
                        foreach (var g in grupos)
                        {
                            var emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == g.Key);
                            ws.Cell(r, 1).Value = g.Key;
                            ws.Cell(r, 2).Value = emp != null ? emp.nombre_completo.ToUpper() : "";

                            ws.Cell(r, 3).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Monday);
                            ws.Cell(r, 4).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Monday);

                            ws.Cell(r, 5).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Tuesday);
                            ws.Cell(r, 6).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Tuesday);

                            ws.Cell(r, 7).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Wednesday);
                            ws.Cell(r, 8).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Wednesday);

                            ws.Cell(r, 9).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Thursday);
                            ws.Cell(r, 10).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Thursday);

                            ws.Cell(r, 11).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Friday);
                            ws.Cell(r, 12).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Friday);

                            ws.Cell(r, 13).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Saturday);
                            ws.Cell(r, 14).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Saturday);

                            ws.Cell(r, 15).Value = GetTurnoTextoSemanal(g.ToList(), DayOfWeek.Sunday);
                            ws.Cell(r, 16).Value = GetSoloEstatus(g.ToList(), DayOfWeek.Sunday);

                            for (int col = 3; col <= 16; col++) { ws.Cell(r, col).Style.Alignment.WrapText = true; ws.Cell(r, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; }
                            r++;
                        }
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(save.FileName);
                        MessageBox.Show("¡Exportado correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); }
        }

        private void ExportarReporteDiario(bool conAsistencia)
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime diaSeleccionado = dpMaestro.SelectedDate.Value.Date;

            DataService.CargarAsignaciones();
            var asignacionesDelDia = DataService.Asignaciones.Where(x => x.fecha.Date == diaSeleccionado).ToList();

            if (!asignacionesDelDia.Any()) { MessageBox.Show($"No hay asignaciones para {diaSeleccionado:dd/MM/yyyy}.", "Aviso"); return; }

            try
            {
                string suffix = conAsistencia ? "ASISTENCIA" : "BASICO";
                var save = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"REPORTE_DIARIO_{suffix}_{diaSeleccionado:yyyyMMdd}.xlsx" };
                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Reporte Diario");

                        var headers = new List<string> { "ID", "FECHA", "EMPLEADO", "UBICACIÓN", "TURNO" };
                        if (conAsistencia) headers.Add("ASISTENCIA");

                        for (int i = 0; i < headers.Count; i++)
                        {
                            ws.Cell(1, i + 1).Value = headers[i];
                            ws.Cell(1, i + 1).Style.Font.Bold = true;
                            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
                            ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        int r = 2;
                        DataService.CargarUbicaciones();

                        foreach (var asig in asignacionesDelDia)
                        {
                            var emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == asig.id_empleado);
                            var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == asig.id_ubicacion);

                            ws.Cell(r, 1).Value = asig.id_empleado;
                            ws.Cell(r, 2).Value = asig.fecha.ToString("dd/MM/yyyy");
                            ws.Cell(r, 3).Value = emp != null ? emp.nombre_completo.ToUpper() : "";
                            ws.Cell(r, 4).Value = ubi?.nombre_lugar?.ToUpper() ?? "SIN UBICACIÓN";
                            ws.Cell(r, 5).Value = FormatearTurnoDiario(asig).Replace("\n", " - ");

                            if (conAsistencia)
                            {
                                ws.Cell(r, 6).Value = FormatearEstatus(asig);
                                ws.Cell(r, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }

                            r++;
                        }
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(save.FileName);
                        MessageBox.Show("¡Diario exportado!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); }
        }

        // ==========================================
        // NAVEGADOR INTELIGENTE (FILTRO EN VIVO)
        // ==========================================
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

        public class FilaVistaPlana
        {
            public int IdEmpleado { get; set; }
            public string Fecha { get; set; }
            public string NombreEmpleado { get; set; }
            public string Ubicacion { get; set; }
            public string Turno { get; set; }
            public string Estatus { get; set; }
        }
    }
}