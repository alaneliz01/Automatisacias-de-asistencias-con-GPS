using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

            // Usamos el evento Loaded para asegurar que la UI esté lista antes de cargar datos
            this.Loaded += (s, e) => {
                LoadEmployees();
                dpMaestro.SelectedDate = DateTime.Today;
            };
        }

        private void IniciarReloj()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            // El formato coincide con tu estética profesional de monitoreo
            _timer.Tick += (s, e) => lblReloj.Text = $"| {DateTime.Now:dd 'de' MMMM | hh:mm:ss tt}".ToUpper();
            _timer.Start();
        }

        private void LoadEmployees()
        {
            try
            {
                DataService.CargarEmpleados();

                // IMPORTANTE: Para que el ComboBox Dark funcione con DisplayMemberPath="nombre_completo",
                // el objeto anónimo debe tener exactamente ese nombre de propiedad.
                var listaConTodos = new List<object> {
                    new { id_empleado = -1, nombre_completo = "-- TODOS LOS EMPLEADOS --" }
                };

                // Ordenamos por nombre para que sea fácil de buscar
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
                // Evitamos que la app truene si falla la DB, pero podrías loguear el error
                Console.WriteLine(ex.Message);
            }
        }

        // --- LÓGICA DE NAVEGACIÓN DE SEMANAS ---

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

                // Lógica para encontrar el lunes de la semana seleccionada
                int diff = (7 + (f.DayOfWeek - DayOfWeek.Monday)) % 7;
                _lunesActual = f.AddDays(-1 * diff).Date;
                _domingoActual = _lunesActual.AddDays(6).Date;

                // Actualizamos el texto del rango (del Lunes al Domingo)
                lblRangoTexto.Text = $"DEL {_lunesActual:dd MMM} AL {_domingoActual:dd MMM}".ToUpper();
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            // Evitamos errores si aún no se cargan datos
            if (dpMaestro.SelectedDate == null) return;

            var dataCruda = DataService.ObtenerAsignacionesDetalladas();

            // 1. Filtro por Empleado (si no es "-- TODOS --")
            if (cbEmpleados.SelectedValue != null && (int)cbEmpleados.SelectedValue != -1)
            {
                int id = (int)cbEmpleados.SelectedValue;
                dataCruda = dataCruda.Where(x => x.id_empleado == id).ToList();
            }

            // 2. Filtro por Rango de Fecha Semanal
            dataCruda = dataCruda.Where(x => x.fecha >= _lunesActual && x.fecha <= _domingoActual).ToList();

            // 3. Agrupación para la vista de tabla (DataGrid)
            var vistaSemanal = dataCruda
                .GroupBy(x => x.id_empleado)
                .Select(g => new FilaVistaSemanal
                {
                    IdEmpleado = g.Key,
                    NombreEmpleado = g.First().empleado.ToUpper(),
                    Lunes = GetTurnoTexto(g.ToList(), DayOfWeek.Monday),
                    Martes = GetTurnoTexto(g.ToList(), DayOfWeek.Tuesday),
                    Miercoles = GetTurnoTexto(g.ToList(), DayOfWeek.Wednesday),
                    Jueves = GetTurnoTexto(g.ToList(), DayOfWeek.Thursday),
                    Viernes = GetTurnoTexto(g.ToList(), DayOfWeek.Friday),
                    Sabado = GetTurnoTexto(g.ToList(), DayOfWeek.Saturday),
                    Domingo = GetTurnoTexto(g.ToList(), DayOfWeek.Sunday),
                    // Contamos turnos reales (excluyendo descansos)
                    TotalSemana = g.Count(t => !t.turno.ToUpper().Contains("LIBRE") &&
                                              !t.turno.ToUpper().Contains("DESC")).ToString() + " Turnos"
                }).ToList();

            dgAsignaciones.ItemsSource = vistaSemanal;
        }

        private string GetTurnoTexto(List<AsignacionDetalle> turnos, DayOfWeek dia)
        {
            var t = turnos.FirstOrDefault(x => x.fecha.DayOfWeek == dia);
            if (t == null) return "-";

            string txt = t.turno.ToUpper();
            // Normalizamos el texto de descanso para que la tabla se vea limpia
            return (txt.Contains("LIBRE") || txt.Contains("DESCANSO")) ? "DESCANSO" : txt;
        }

        private void CbEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            cbEmpleados.SelectedIndex = 0;
            dpMaestro.SelectedDate = DateTime.Today;
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();

        // --- EXPORTACIÓN A EXCEL (Usando ClosedXML) ---

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (dgAsignaciones.ItemsSource == null || !(dgAsignaciones.ItemsSource as List<FilaVistaSemanal>).Any())
            {
                MessageBox.Show("No hay datos para exportar en esta semana.", "Aviso");
                return;
            }

            string nombreSeleccionado = cbEmpleados.Text;


            if (string.IsNullOrEmpty(nombreSeleccionado) && cbEmpleados.SelectedItem != null)
            {
                dynamic selectedItem = cbEmpleados.SelectedItem;
                nombreSeleccionado = selectedItem.nombre_completo;
            }

            string mensaje = $"¿Desea exportar el reporte de: {nombreSeleccionado}?";

            if (MessageBox.Show(mensaje, "Confirmar Exportación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ExportarProceso();
            }
        }

        private void ExportarProceso()
        {
            try
            {
                var items = dgAsignaciones.ItemsSource as List<FilaVistaSemanal>;
                var save = new SaveFileDialog
                {
                    Filter = "Excel|*.xlsx",
                    FileName = $"SECORVI_Reporte_{_lunesActual:dd-MM-yyyy}.xlsx"
                };

                if (save.ShowDialog() == true)
                {
                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Reporte Asistencia");

                        // Encabezados con estilo
                        string[] h = { "ID", "EMPLEADO", "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO", "TOTAL" };
                        for (int i = 0; i < h.Length; i++)
                        {
                            var cell = ws.Cell(1, i + 1);
                            cell.Value = h[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFB300"); // Tu color amarillo
                        }

                        // Llenado de filas
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

                        ws.Columns().AdjustToContents(); // Auto-ajuste de columnas
                        wb.SaveAs(save.FileName);
                        MessageBox.Show("¡Reporte generado con éxito!", "Secorvi System", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar Excel: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Clase de soporte para la vista
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