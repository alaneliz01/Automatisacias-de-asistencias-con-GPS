using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Secorvi
{
    public partial class CalendarioEmpleado : Page
    {
        private Empleado _empleado;
        private DateTime _lunesActual;
        private const double FILA_ALTURA = 45.0;
        private Border _startCell;
        private bool _dragging = false;
        private List<DateTime> _fechasSeleccionadas = new List<DateTime>();
        private ContextMenu _menuContexto;

        public CalendarioEmpleado(Empleado emp)
        {
            InitializeComponent();
            _empleado = emp;

            // Cultura MX para nombres de días y meses
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("es-MX");

            // Cálculo del lunes de la semana actual
            _lunesActual = DateTime.Now.Date.AddDays(-(int)(DateTime.Now.Date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)DateTime.Now.Date.DayOfWeek - 1));

            CrearMenuContexto();
            this.Loaded += (s, e) => {
                if (dpSaltoFecha != null) dpSaltoFecha.SelectedDate = _lunesActual;
                // Sincronizado con nombre_completo (minúsculas)
                if (lblNombreEmpleado != null) lblNombreEmpleado.Text = _empleado?.nombre_completo?.ToUpper();
                IniciarReloj();
                DibujarGrid();
                ActualizarVista();
            };
        }

        private void IniciarReloj()
        {
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => {
                if (lblFechaHoraActual != null) lblFechaHoraActual.Text = $"| {DateTime.Now:dd 'de' MMMM | hh:mm:ss tt}".ToUpper();
            };
            timer.Start();
        }

        private void DibujarGrid()
        {
            if (GridCalendario == null) return;
            GridCalendario.RowDefinitions.Clear();
            GridCalendario.ColumnDefinitions.Clear();
            GridCalendario.Children.Clear();

            GridCalendario.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            for (int i = 0; i < 7; i++) GridCalendario.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            GridCalendario.RowDefinitions.Add(new RowDefinition { Height = new GridLength(FILA_ALTURA) });

            for (int d = 1; d <= 7; d++)
            {
                DateTime fDia = _lunesActual.AddDays(d - 1);
                Border header = new Border { Background = new SolidColorBrush(Color.FromRgb(34, 45, 54)), BorderThickness = new Thickness(0.5), BorderBrush = new SolidColorBrush(Color.FromRgb(40, 47, 60)) };
                header.Child = new TextBlock { Text = fDia.ToString("ddd").ToUpper() + "\n" + fDia.ToString("dd MMM"), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, FontSize = 10, FontWeight = FontWeights.Bold };
                Grid.SetRow(header, 0); Grid.SetColumn(header, d); GridCalendario.Children.Add(header);
            }

            for (int h = 0; h < 24; h++)
            {
                GridCalendario.RowDefinitions.Add(new RowDefinition { Height = new GridLength(FILA_ALTURA) });
                TextBlock tH = new TextBlock { Text = DateTime.Today.AddHours(h).ToString("hh:mm tt").ToLower(), Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(tH, h + 1); Grid.SetColumn(tH, 0); GridCalendario.Children.Add(tH);
                for (int d = 1; d <= 7; d++)
                {
                    Border c = new Border { Background = Brushes.Transparent, BorderBrush = new SolidColorBrush(Color.FromRgb(40, 47, 60)), BorderThickness = new Thickness(0.5), Tag = _lunesActual.AddDays(d - 1).Date.AddHours(h), Cursor = Cursors.Hand };
                    c.PreviewMouseLeftButtonDown += Celda_PreviewMouseLeftButtonDown;
                    c.PreviewMouseMove += Celda_PreviewMouseMove;
                    c.PreviewMouseLeftButtonUp += Celda_PreviewMouseLeftButtonUp;
                    Grid.SetRow(c, h + 1); Grid.SetColumn(c, d); GridCalendario.Children.Add(c);
                }
            }
        }

        public void CargarAsignaciones()
        {
            if (GridCalendario == null) return;

            // 1. Limpiar visuales
            var paraEliminar = GridCalendario.Children.OfType<Border>().Where(b => b.Tag?.ToString() == "VISUAL_ASIG").ToList();
            foreach (var b in paraEliminar) GridCalendario.Children.Remove(b);

            // 2. Cargar desde DB
            DataService.ActualizarTodo();

            // 3. Filtrar (Sincronizado con minúsculas: id_empleado, fecha)
            var asignaciones = DataService.Asignaciones.Where(a =>
                a.id_empleado == _empleado.id_empleado &&
                a.fecha.Date >= _lunesActual.Date &&
                a.fecha.Date <= _lunesActual.AddDays(6).Date).ToList();

            int servicios = 0; int descansos = 0; int vacaciones = 0;

            if (lblRangoSemana != null) lblRangoSemana.Text = $"{_lunesActual:dd MMM} - {_lunesActual.AddDays(6):dd MMM}".ToUpper();

            foreach (var asig in asignaciones)
            {
                // Sincronizado con: id_turno (minúscula), id_lugar (minúscula), estatus (minúscula)
                var turno = DataService.Turnos.FirstOrDefault(t => t.id_turno == asig.id_turno);
                var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.id_lugar == asig.id_ubicacion);

                if (turno == null) continue;

                if (asig.estatus == "VACACIONES") vacaciones++;
                else if (asig.estatus == "DÍA LIBRE") descansos++;
                else servicios++;

                Color colorBase = asig.estatus switch
                {
                    "VACACIONES" => Color.FromRgb(52, 152, 219),
                    "DÍA LIBRE" => Color.FromRgb(55, 65, 81),
                    _ => Color.FromRgb(192, 57, 43)
                };

                Border bloque = new Border
                {
                    Background = new SolidColorBrush(colorBase),
                    CornerRadius = new CornerRadius(2),
                    Tag = "VISUAL_ASIG",
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(2)
                };

                bloque.MouseLeftButtonDown += (s, ev) => {
                    ev.Handled = true;
                    _fechasSeleccionadas = new List<DateTime> { asig.fecha };
                    _menuContexto.Placement = PlacementMode.MousePoint;
                    _menuContexto.IsOpen = true;
                };

                string texto = asig.estatus switch
                {
                    "VACACIONES" => "VACACIONES",
                    "DÍA LIBRE" => "DESCANSO",
                    _ => $"{ubi?.nombre_lugar ?? "PUNTO"}\n{DateTime.Today.Add(turno.hora_inicio):hh:mm tt} - {DateTime.Today.Add(turno.hora_fin):hh:mm tt}".ToUpper()
                };

                bloque.Child = new TextBlock { Text = texto, Foreground = Brushes.White, FontSize = 7.5, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };

                int col = (int)asig.fecha.DayOfWeek;
                col = (col == 0) ? 7 : col;
                int row = turno.hora_inicio.Hours + 1;
                int span = (int)(turno.hora_fin - turno.hora_inicio).TotalHours;
                if (span <= 0) span = 1;

                Grid.SetColumn(bloque, col); Grid.SetRow(bloque, row); Grid.SetRowSpan(bloque, span);
                GridCalendario.Children.Add(bloque);
            }

            // Actualizar etiquetas de contadores
            if (lblTotalServicios != null) lblTotalServicios.Text = servicios.ToString();
            if (lblTotalDescansos != null) lblTotalDescansos.Text = descansos.ToString();
            if (lblTotalVacaciones != null) lblTotalVacaciones.Text = vacaciones.ToString();
        }

        private void CrearMenuContexto()
        {
            _menuContexto = new ContextMenu();
            var itemEli = new MenuItem { Header = "ELIMINAR BLOQUE" }; itemEli.Click += (s, e) => EjecutarEliminacionSilenciosa();
            var itemLibre = new MenuItem { Header = "ASIGNAR DESCANSO" }; itemLibre.Click += (s, e) => AsignarEstadoEspecial("DÍA LIBRE");
            var itemVac = new MenuItem { Header = "VACACIONES" }; itemVac.Click += (s, e) => AsignarEstadoEspecial("VACACIONES");
            _menuContexto.Items.Add(itemEli); _menuContexto.Items.Add(itemLibre); _menuContexto.Items.Add(itemVac);
        }

        private void EjecutarEliminacionSilenciosa()
        {
            foreach (var f in _fechasSeleccionadas.Select(x => x.Date).Distinct())
                DataService.EliminarAsignacionPorFecha(_empleado.id_empleado, f);
            ActualizarVista();
        }

        private void AsignarEstadoEspecial(string t)
        {
            foreach (var f in _fechasSeleccionadas.Select(x => x.Date).Distinct())
            {
                DataService.EliminarAsignacionPorFecha(_empleado.id_empleado, f);
                // Sincronizado con: nombre (minúscula) e id_turno (minúscula)
                var tE = DataService.Turnos.FirstOrDefault(x => x.nombre == t);
                if (tE != null)
                    DataService.CrearAsignacion(new Asignacion { id_empleado = _empleado.id_empleado, id_ubicacion = 0, id_turno = tE.id_turno, fecha = f, estatus = t });
            }
            ActualizarVista();
        }

        // Lógica de Selección por Arrastre (Dragging)
        private void Celda_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { _dragging = true; _startCell = sender as Border; Mouse.Capture(GridCalendario, CaptureMode.SubTree); LimpiarSeleccionVisual(); }
        private void Celda_PreviewMouseMove(object sender, MouseEventArgs e) { if (_dragging && _startCell != null && sender is Border c) SeleccionarRango((DateTime)_startCell.Tag, (DateTime)c.Tag); }
        private void Celda_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { _dragging = false; Mouse.Capture(null); if (_fechasSeleccionadas.Count > 0) { _menuContexto.Placement = PlacementMode.MousePoint; _menuContexto.IsOpen = true; } }

        private void SeleccionarRango(DateTime inicio, DateTime fin)
        {
            LimpiarSeleccionVisual();
            foreach (Border b in GridCalendario.Children.OfType<Border>().Where(x => x.Tag is DateTime))
            {
                DateTime dt = (DateTime)b.Tag;
                if (dt >= (inicio < fin ? inicio : fin) && dt <= (inicio < fin ? fin : inicio)) { b.Background = new SolidColorBrush(Color.FromArgb(100, 255, 179, 0)); _fechasSeleccionadas.Add(dt); }
            }
        }

        private void LimpiarSeleccionVisual() { foreach (Border b in GridCalendario.Children.OfType<Border>().Where(x => x.Tag is DateTime)) b.Background = Brushes.Transparent; _fechasSeleccionadas.Clear(); }
        private void ActualizarVista() { CargarAsignaciones(); }

        private void DpSaltoFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { if (dpSaltoFecha?.SelectedDate != null) { DateTime f = dpSaltoFecha.SelectedDate.Value; _lunesActual = f.AddDays(-(int)(f.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)f.DayOfWeek - 1)); DibujarGrid(); ActualizarVista(); } }

        // Sincronizado con: id_empleado (minúscula)
        private void BtnAsignarHorario_Click(object sender, RoutedEventArgs e) { Mapa win = new Mapa(_empleado.id_empleado, _lunesActual); win.Owner = Window.GetWindow(this); if (win.ShowDialog() == true) ActualizarVista(); }

        private void BtnVolver_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
        private void BtnSemanaAtras_Click(object sender, RoutedEventArgs e) { _lunesActual = _lunesActual.AddDays(-7); DibujarGrid(); ActualizarVista(); }
        private void BtnSemanaAdelante_Click(object sender, RoutedEventArgs e) { _lunesActual = _lunesActual.AddDays(7); DibujarGrid(); ActualizarVista(); }

        // MÉTODOS DE MENÚ CONTEXTO (Copiar y Limpiar)
        private void MenuItem_CopiarDia_Click(object sender, RoutedEventArgs e)
        {
            if (_fechasSeleccionadas.Count > 0)
            {
                DateTime fechaOrigen = _fechasSeleccionadas.First().Date;
                DateTime fechaDestino = fechaOrigen.AddDays(1);
                var asigOriginal = DataService.Asignaciones.FirstOrDefault(a => a.id_empleado == _empleado.id_empleado && a.fecha.Date == fechaOrigen);

                if (asigOriginal != null)
                {
                    DataService.EliminarAsignacionPorFecha(_empleado.id_empleado, fechaDestino);
                    var copia = new Asignacion { id_empleado = asigOriginal.id_empleado, id_ubicacion = asigOriginal.id_ubicacion, id_turno = asigOriginal.id_turno, fecha = fechaDestino, estatus = asigOriginal.estatus };
                    DataService.CrearAsignacion(copia);
                    ActualizarVista();
                    MessageBox.Show("Turno copiado al día siguiente.");
                }
            }
        }

        private void MenuItem_LimpiarDia_Click(object sender, RoutedEventArgs e) { EjecutarEliminacionSilenciosa(); }
    }
}