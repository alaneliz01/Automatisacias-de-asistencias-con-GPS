using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks; // Asegúrate de tener este para Task.Run
using Microsoft.Win32;
using ClosedXML.Excel;
using Secorvi.Models;

namespace Secorvi
{
    public partial class Turnos : Page
    {
        private DispatcherTimer _timer;
        private DateTime _lunesActual;
        private DateTime _domingoActual;

        public Turnos()
        {
            InitializeComponent();
            IniciarReloj();

            this.Loaded += (s, e) => {
                LoadEmployees();
                dpMaestro.SelectedDate = DateTime.Today;
            };
            this.Unloaded += (s, e) =>
            {
                if (_timer != null)
                {
                    _timer.Stop();
                }
            };
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
                var listaConTodos = new List<object> {
                    new { id_empleado = -1, nombre_completo = "-- TODOS LOS EMPLEADOS --" }
                };

                var empleadosOrdenados = DataService.Empleados
                    .OrderBy(x => x.nombre_completo)
                    .Select(e => new { e.id_empleado, nombre_completo = e.nombre_completo.ToUpper() });

                foreach (var emp in empleadosOrdenados)
                {
                    listaConTodos.Add(emp);
                }

                cbEmpleados.ItemsSource = listaConTodos;
                cbEmpleados.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void BtnSemanaAtras_Click(object sender, RoutedEventArgs e)
        {
            DateTime fechaActual = dpMaestro.SelectedDate ?? DateTime.Today;
            dpMaestro.SelectedDate = fechaActual.AddDays(-7);
        }

        private void BtnSemanaAdelante_Click(object sender, RoutedEventArgs e)
        {
            DateTime fechaActual = dpMaestro.SelectedDate ?? DateTime.Today;
            dpMaestro.SelectedDate = fechaActual.AddDays(7);
        }

        private void DpMaestro_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpMaestro.SelectedDate.HasValue)
            {
                DateTime f = dpMaestro.SelectedDate.Value;
                int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
                _lunesActual = f.AddDays(-1 * diff).Date;
                _domingoActual = _lunesActual.AddDays(6).Date;

                lblRangoTexto.Text = $"DEL {_lunesActual:dd MMM} AL {_domingoActual:dd MMM}".ToUpper();
                ApplyFilter();
            }
        }

        private async void ApplyFilter()
        {
            if (dpMaestro.SelectedDate == null) return;

            dgAsignaciones.ItemsSource = null;

            int idEmpleadoFiltro = cbEmpleados.SelectedValue != null ? (int)cbEmpleados.SelectedValue : -1;
            DateTime lunes = _lunesActual;
            DateTime domingo = _domingoActual;

            var vistaSemanal = await Task.Run(() =>
            {
                DataService.CargarAsignaciones();
                DataService.CargarEmpleados();

                var asignaciones = DataService.Asignaciones
                    .Where(x => x.fecha >= lunes && x.fecha <= domingo)
                    .ToList();

                if (idEmpleadoFiltro != -1)
                {
                    asignaciones = asignaciones.Where(x => x.id_empleado == idEmpleadoFiltro).ToList();
                }

                var resultado = new List<FilaVistaSemanal>();
                var grupos = asignaciones.GroupBy(x => x.id_empleado);

                foreach (var g in grupos)
                {
                    var empleadoInfo = DataService.Empleados.FirstOrDefault(e => e.id_empleado == g.Key);
                    if (empleadoInfo == null) continue;

                    var fila = new FilaVistaSemanal
                    {
                        IdEmpleado = g.Key,
                        NombreEmpleado = empleadoInfo.nombre_completo.ToUpper(),
                        Lunes = GetTurnoTexto(g.ToList(), DayOfWeek.Monday),
                        Martes = GetTurnoTexto(g.ToList(), DayOfWeek.Tuesday),
                        Miercoles = GetTurnoTexto(g.ToList(), DayOfWeek.Wednesday),
                        Jueves = GetTurnoTexto(g.ToList(), DayOfWeek.Thursday),
                        Viernes = GetTurnoTexto(g.ToList(), DayOfWeek.Friday),
                        Sabado = GetTurnoTexto(g.ToList(), DayOfWeek.Saturday),
                        Domingo = GetTurnoTexto(g.ToList(), DayOfWeek.Sunday)
                    };

                    // PEGA ESTO:
                    var diasSemana = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                    };

                    int total = diasSemana.Count(dia => {
                        // Usamos la misma lógica visual para contar: un turno por día máximo
                        string estado = GetTurnoTexto(g.ToList(), dia);
                        return estado != "DESCANSO" && estado != "VACACIONES" && estado != "-";
                    });

                    fila.TotalSemana = $"{total} Turnos";
                    resultado.Add(fila);
                }

                return resultado.OrderBy(x => x.NombreEmpleado).ToList();
            });

            dgAsignaciones.ItemsSource = vistaSemanal;
        }

        private string GetTurnoTexto(List<Asignacion> turnos, DayOfWeek dia)
        {
            var t = turnos.FirstOrDefault(x => x.fecha.DayOfWeek == dia);
            if (t == null) return "-";

            string estatusNorm = t.estatus?.Trim().ToUpper() ?? "";
            string descNorm = t.descripcion_del_turno?.Trim().ToUpper() ?? "";

            bool esVacacion = estatusNorm == "VACACIONES" || descNorm == "VACACIONES";
            bool esDescanso = estatusNorm == "DÍA LIBRE" || descNorm == "DÍA LIBRE" || estatusNorm == "DESCANSO" || descNorm.Contains("LIBRE") || descNorm.Contains("DESC");

            if (esVacacion) return "VACACIONES";
            if (esDescanso) return "DESCANSO";

            if (t.hora_inicio == TimeSpan.Zero && t.hora_fin == TimeSpan.Zero)
            {
                return "24 HORAS";
            }

            return $"{t.hora_inicio:hh\\:mm} - {t.hora_fin:hh\\:mm}";
        }

        private void CbEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            cbEmpleados.SelectedIndex = 0;
            dpMaestro.SelectedDate = DateTime.Today;
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (dgAsignaciones.ItemsSource == null || !(dgAsignaciones.ItemsSource as List<FilaVistaSemanal>).Any())
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso");
                return;
            }

            string nombreSeleccionado = cbEmpleados.Text;
            if (MessageBox.Show($"¿Desea exportar el reporte?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ExportarProceso();
            }
        }

        private void ExportarProceso()
        {
            try
            {
                var items = dgAsignaciones.ItemsSource as List<FilaVistaSemanal>;
                var save = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"SECORVI_REPORTE.xlsx" };

                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Reporte");
                        string[] h = { "ID", "EMPLEADO", "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO", "TOTAL" };
                        for (int i = 0; i < h.Length; i++)
                        {
                            var cell = ws.Cell(1, i + 1);
                            cell.Value = h[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFB300");
                        }

                        int r = 2;
                        foreach (var i in items)
                        {
                            ws.Cell(r, 1).Value = i.IdEmpleado;
                            ws.Cell(r, 2).Value = i.NombreEmpleado;
                            ws.Cell(r, 3).Value = i.Lunes;
                            ws.Cell(r, 4).Value = i.Martes;
                            ws.Cell(r, 5).Value = i.Miercoles;
                            ws.Cell(r, 6).Value = i.Jueves;
                            ws.Cell(r, 7).Value = i.Viernes;
                            ws.Cell(r, 8).Value = i.Sabado;
                            ws.Cell(r, 9).Value = i.Domingo;
                            ws.Cell(r, 10).Value = i.TotalSemana;
                            r++;
                        }
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(save.FileName);
                        MessageBox.Show("¡Exportado!");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }

    // CLASE DE SOPORTE (Crucial para que no marque error)
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
        public string TotalSemana { get; set; }
    }
}