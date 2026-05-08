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

            // Sincronizando la info local con la estructura del Excel: [Fecha] \n [Turno] \n [Lugar] \n [Estatus]
            var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == t.id_ubicacion);
            string lugar = ubi?.nombre_lugar?.ToUpper() ?? "SIN UBICACIÓN";
            string fecha = t.fecha.ToString("dd/MMM").ToUpper();
            string turno = FormatearTurnoDiario(t);
            string estatus = FormatearEstatus(t).Replace("✅", "").Replace("⏳", "").Replace("🚪", "").Replace("❌", "").Replace("🏖️", "").Replace("💤", "").Trim();

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

            string estatusNorm = t.estatus?.Trim().ToUpper() ?? "";
            string descNorm = t.descripcion_del_turno?.Trim().ToUpper() ?? "";

            if (estatusNorm == "VACACIONES" || descNorm == "VACACIONES") return "🏖️ VACACIONES";
            if (estatusNorm == "DÍA LIBRE" || estatusNorm == "DESCANSO" || descNorm.Contains("LIBRE") || descNorm.Contains("DESC")) return "💤 DESCANSO";

            if (estatusNorm == "COMPLETADO" || estatusNorm == "ASISTENCIA" || estatusNorm == "ASISTIÓ") return "✅ ASISTIÓ";
            if (estatusNorm == "SALIDA") return "🚪 FINALIZADO";
            if (estatusNorm == "FALTA") return "❌ FALTÓ";

            return "⏳ PENDIENTE";
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
        // EXPORTACIÓN A EXCEL (DISEÑO MEJORADO)
        // ==========================================
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportacionReportes();
        }

        // EXPORTACIÓN A EXCEL (ESTILO MATRIZ / SECORVI)
        private void ExportacionReportes()
        {
            if (dpMaestro.SelectedDate == null) return;
            DateTime f = dpMaestro.SelectedDate.Value;

            // Ajustamos el inicio de la semana (Ej. Nómina empezando en Viernes o Lunes)
            int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime inicioSemana = f.AddDays(-1 * diff).Date;
            DateTime finSemana = inicioSemana.AddDays(6).Date;

            DataService.CargarAsignaciones();
            DataService.CargarUbicaciones();

            // Traemos todos los datos de la semana seleccionada
            var datos = DataService.Asignaciones.Where(x => x.fecha >= inicioSemana && x.fecha <= finSemana).ToList();
            if (!datos.Any()) { MessageBox.Show("No hay datos en esta semana.", "Aviso"); return; }

            try
            {
                var save = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"REPORTE_LISTA_{inicioSemana:yyyyMMdd}.xlsx" };
                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Reporte Diario");
                        int filaActual = 1;

                        // 1. CABECERAS (Basado en la imagen)
                        ws.Cell(filaActual, 1).Value = "Nombre del empleado";
                        ws.Cell(filaActual, 2).Value = "Dia";
                        ws.Cell(filaActual, 3).Value = "Turno";
                        ws.Cell(filaActual, 4).Value = "Lugar";
                        ws.Cell(filaActual, 5).Value = "Estatus";

                        // Estilo básico para las cabeceras
                        var rangoCabeceras = ws.Range(filaActual, 1, filaActual, 5);
                        rangoCabeceras.Style.Font.Bold = true;
                        rangoCabeceras.SetAutoFilter();

                        ws.SheetView.FreezeRows(1); // Congelar la primera fila

                        filaActual++;

                        // 2. PREPARAR DATOS 
                        var listadoPlano = datos
                            .Select(d => new
                            {
                                Asignacion = d,
                                Emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == d.id_empleado),
                                Ubi = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == d.id_ubicacion)
                            })
                            .OrderBy(x => x.Emp?.nombre_completo)
                            .ThenBy(x => x.Asignacion.fecha)
                            .ToList();

                        // 3. LLENADO DE LA LISTA
                        foreach (var item in listadoPlano)
                        {
                            ws.Cell(filaActual, 1).Value = item.Emp?.nombre_completo ?? "N/A";
                            ws.Cell(filaActual, 2).Value = item.Asignacion.fecha.ToString("dd/MM/yyyy");
                            ws.Cell(filaActual, 3).Value = FormatearTurnoDiario(item.Asignacion); 
                            ws.Cell(filaActual, 4).Value = item.Ubi?.nombre_lugar ?? "SIN ASIGNAR";
                            ws.Cell(filaActual, 5).Value = FormatearEstatus(item.Asignacion);

                            // Bordes simples
                            ws.Range(filaActual, 1, filaActual, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            ws.Range(filaActual, 1, filaActual, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                            filaActual++;
                        }

                        // 4. AJUSTE DE TAMAÑOS
                        ws.Columns().AdjustToContents();
                        ws.Column(1).Width = 35; 
                        ws.Column(4).Width = 25; 

                        wb.SaveAs(save.FileName);
                        MessageBox.Show("Reporte exportado con éxito.", "Excelente", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al exportar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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