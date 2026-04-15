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

        // Variables para la selección táctica (Drag)
        private Border _startCell;
        private bool _dragging = false;
        private List<Border> _celdasSeleccionadasVisual = new List<Border>();
        private List<DateTime> _fechasSeleccionadas = new List<DateTime>();
        private ContextMenu _menuContexto;

        public CalendarioEmpleado(Empleado emp)
        {
            InitializeComponent();
            _empleado = emp;

            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("es-MX");
            _lunesActual = DateTime.Now.Date.AddDays(-(int)(DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)DateTime.Now.DayOfWeek - 1));

            CrearMenuContexto();

            this.Loaded += (s, e) => {
                if (dpSaltoFecha != null) dpSaltoFecha.SelectedDate = _lunesActual;
                if (lblNombreEmpleado != null) lblNombreEmpleado.Text = _empleado?.Nombre?.ToUpper() ?? "AGENTE";

                IniciarReloj();
                DibujarGrid();
                ActualizarVista();
            };
        }

        private void IniciarReloj()
        {
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => {
                if (lblFechaHoraActual != null)
                    lblFechaHoraActual.Text = $"| {DateTime.Now:dd 'de' MMMM | hh:mm:ss tt}".ToUpper();
            };
            timer.Start();
        }

        private void DibujarGrid()
        {
            if (GridCalendario == null) return;
            GridCalendario.RowDefinitions.Clear();
            GridCalendario.ColumnDefinitions.Clear();
            GridCalendario.Children.Clear();

            // Horas (80px) + 7 días (proporcional)
            GridCalendario.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            for (int i = 0; i < 7; i++)
                GridCalendario.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            GridCalendario.RowDefinitions.Add(new RowDefinition { Height = new GridLength(FILA_ALTURA) });

            // Cabeceras de días
            for (int d = 1; d <= 7; d++)
            {
                DateTime fDia = _lunesActual.AddDays(d - 1);
                Border header = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(34, 45, 54)),
                    BorderThickness = new Thickness(0.5),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(40, 47, 60))
                };
                header.Child = new TextBlock
                {
                    Text = fDia.ToString("ddd").ToUpper() + "\n" + fDia.ToString("dd MMM"),
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                };
                Grid.SetRow(header, 0); Grid.SetColumn(header, d);
                GridCalendario.Children.Add(header);
            }

            // Cuerpo: 24 Horas
            for (int h = 0; h < 24; h++)
            {
                GridCalendario.RowDefinitions.Add(new RowDefinition { Height = new GridLength(FILA_ALTURA) });
                TextBlock tH = new TextBlock
                {
                    Text = DateTime.Today.AddHours(h).ToString("hh:mm tt").ToLower(),
                    Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(tH, h + 1); Grid.SetColumn(tH, 0);
                GridCalendario.Children.Add(tH);

                for (int d = 1; d <= 7; d++)
                {
                    Border c = new Border
                    {
                        Background = Brushes.Transparent,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(40, 47, 60)),
                        BorderThickness = new Thickness(0.5),
                        Tag = _lunesActual.AddDays(d - 1).Date.AddHours(h),
                        Cursor = Cursors.Hand
                    };
                    c.PreviewMouseLeftButtonDown += Celda_PreviewMouseLeftButtonDown;
                    c.PreviewMouseMove += Celda_PreviewMouseMove;
                    c.PreviewMouseLeftButtonUp += Celda_PreviewMouseLeftButtonUp;
                    Grid.SetRow(c, h + 1); Grid.SetColumn(c, d);
                    GridCalendario.Children.Add(c);
                }
            }
        }

        private void Celda_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging && _startCell != null && sender is Border c)
            {
                if (c.Tag is DateTime fA)
                    SeleccionarRango((DateTime)_startCell.Tag, fA);
            }
        }

        private void SeleccionarRango(DateTime inicio, DateTime fin)
        {
            _fechasSeleccionadas.Clear();
            _celdasSeleccionadasVisual.Clear();

            var cI = GridCalendario.Children.OfType<Border>().FirstOrDefault(b => b.Tag is DateTime dt && dt == inicio);
            var cF = GridCalendario.Children.OfType<Border>().FirstOrDefault(b => b.Tag is DateTime dt && dt == fin);
            if (cI == null || cF == null) return;

            int minCol = Math.Min(Grid.GetColumn(cI), Grid.GetColumn(cF));
            int maxCol = Math.Max(Grid.GetColumn(cI), Grid.GetColumn(cF));
            int minRow = Math.Min(Grid.GetRow(cI), Grid.GetRow(cF));
            int maxRow = Math.Max(Grid.GetRow(cI), Grid.GetRow(cF));

            foreach (Border child in GridCalendario.Children.OfType<Border>().Where(x => x.Tag is DateTime))
            {
                int c = Grid.GetColumn(child);
                int r = Grid.GetRow(child);
                if (c >= minCol && c <= maxCol && r >= minRow && r <= maxRow)
                {
                    child.Background = new SolidColorBrush(Color.FromArgb(120, 52, 152, 219));
                    _celdasSeleccionadasVisual.Add(child);
                    _fechasSeleccionadas.Add((DateTime)child.Tag);
                }
                else child.Background = Brushes.Transparent;
            }
        }

        public void CargarAsignaciones()
        {
            if (GridCalendario == null || GridCalendario.ColumnDefinitions.Count < 8) return;

            // 1. Limpieza de bloques visuales anteriores
            var paraEliminar = GridCalendario.Children.OfType<Border>().Where(b => b.Tag?.ToString() == "VISUAL_ASIG").ToList();
            foreach (var b in paraEliminar) GridCalendario.Children.Remove(b);

            // 2. Obtener asignaciones de la semana
            var asignaciones = DataService.Asignaciones.Where(a =>
                a.IdEmpleado == _empleado.Id && a.Fecha.Date >= _lunesActual.Date && a.Fecha.Date <= _lunesActual.AddDays(6).Date).ToList();

            // Agrupamos por fecha para manejar servicios encimados (sub-columnas)
            var gruposPorDia = asignaciones.GroupBy(a => a.Fecha.Date);

            foreach (var grupo in gruposPorDia)
            {
                var listaAsig = grupo.OrderBy(a => a.Id).ToList();
                int totalEnDia = listaAsig.Count;
                int col = (int)grupo.Key.DayOfWeek;
                col = (col == 0) ? 7 : col; // Domingo es 7

                double colWidth = GridCalendario.ColumnDefinitions[col].ActualWidth;
                if (colWidth <= 0) colWidth = 145; // Ancho base de seguridad

                for (int i = 0; i < totalEnDia; i++)
                {
                    var asig = listaAsig[i];
                    var turno = DataService.Turnos.FirstOrDefault(t => t.Id == asig.IdTurno);
                    var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.Id == asig.IdUbicacion);
                    if (turno == null) continue;

                    // --- PALETA DE COLORES SECORVI ---
                    Color colorBase = asig.Estatus switch
                    {
                        "VACACIONES" => Color.FromRgb(41, 128, 185), // Azul
                        "DÍA LIBRE" => Color.FromRgb(55, 65, 81),   // GRIS CARBÓN (NUEVO)
                        _ => Color.FromRgb(192, 57, 43)  // Rojo (Servicio)
                    };

                    Border bloque = new Border
                    {
                        Background = new SolidColorBrush(colorBase),
                        CornerRadius = new CornerRadius(2),
                        Tag = "VISUAL_ASIG",
                        BorderThickness = new Thickness(1, 1, 1, 3), // Sombra inferior para relieve
                        BorderBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                        SnapsToDevicePixels = true,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        // Si hay varios servicios el mismo día, se dividen el ancho
                        Width = (colWidth / totalEnDia),
                        Margin = new Thickness(i * (colWidth / totalEnDia), 1, 0, 1)
                    };

                    // --- LÓGICA DE ELIMINACIÓN Y ADVERTENCIA ---
                    bloque.MouseLeftButtonDown += (s, ev) => {
                        ev.Handled = true;

                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            // Borrado relámpago
                            DataService.Asignaciones.Remove(asig);
                            DataService.GuardarTodo();
                            lblStatus.Text = "LOG: ELIMINACIÓN RÁPIDA EJECUTADA";
                            ActualizarVista();
                        }
                        else
                        {
                            _fechasSeleccionadas = new List<DateTime> { asig.Fecha };
                            _menuContexto.Placement = PlacementMode.MousePoint;
                            _menuContexto.IsOpen = true;
                        }
                    };

                    bloque.Child = new TextBlock
                    {
                        Text = asig.Estatus == "PROGRAMADO" ? $"{ubi?.Nombre}\n{turno.HorarioTexto}" : asig.Estatus,
                        Foreground = Brushes.White,
                        FontSize = totalEnDia > 1 ? 7 : 8.5,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };

                    // Posicionamiento en el Grid
                    int row = turno.HoraInicio.Hours + 1;
                    int span = Math.Max(1, (int)(turno.HoraFin - turno.HoraInicio).TotalHours);
                    if (span < 0) span = 24 + span;

                    // Seguridad: No salirse de las filas existentes
                    if (row < GridCalendario.RowDefinitions.Count)
                    {
                        Grid.SetColumn(bloque, col);
                        Grid.SetRow(bloque, row);
                        Grid.SetRowSpan(bloque, Math.Min(span, GridCalendario.RowDefinitions.Count - row));
                        GridCalendario.Children.Add(bloque);
                    }
                }
            }
        }

        private void CrearMenuContexto()
        {
            _menuContexto = new ContextMenu();

            var itemEli = new MenuItem
            {
                Header = "DESVINCULAR BLOQUE",
                Icon = new TextBlock { Text = "🗑️", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }
            };
            itemEli.Click += (s, e) => EjecutarEliminacionSilenciosa();
            _menuContexto.Items.Add(itemEli);

            _menuContexto.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(45, 50, 62)), Margin = new Thickness(5, 2, 5, 2) });

            var itemLibre = new MenuItem
            {
                Header = "ASIGNAR DESCANSO",
                Icon = new TextBlock { Text = "🆓", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }
            };
            itemLibre.Click += (s, e) => AsignarEstadoEspecial("DÍA LIBRE");
            _menuContexto.Items.Add(itemLibre);

            var itemVac = new MenuItem
            {
                Header = "PERIODO VACACIONAL",
                Icon = new TextBlock { Text = "🏖️", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }
            };
            itemVac.Click += (s, e) => AsignarEstadoEspecial("VACACIONES");
            _menuContexto.Items.Add(itemVac);
        }

        private void EjecutarEliminacionSilenciosa()
        {
            var dias = _fechasSeleccionadas.Select(f => f.Date).Distinct().ToList();
            int cantidad = DataService.Asignaciones.Count(a => a.IdEmpleado == _empleado.Id && dias.Contains(a.Fecha.Date));

            if (cantidad == 0) return;

            // SEÑAL DE ADVERTENCIA
            MessageBoxResult resultado = MessageBox.Show(
                $"¿Está seguro de eliminar {cantidad} bloque(s) de servicio?\n\nEsta acción no se puede deshacer.",
                "ADVERTENCIA DE SEGURIDAD",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                DataService.Asignaciones.RemoveAll(a => a.IdEmpleado == _empleado.Id && dias.Contains(a.Fecha.Date));
                DataService.GuardarTodo();

                lblStatus.Text = $"LOG: {cantidad} BLOQUE(S) ELIMINADO(S)";
                lblStatus.Foreground = Brushes.OrangeRed; // Cambiamos color para indicar cambio fuerte

                LimpiarSeleccionVisual();
                ActualizarVista();
            }
        }

        private void AsignarEstadoEspecial(string t)
        {
            var dias = _fechasSeleccionadas.Select(c => c.Date).Distinct().ToList();

            // Contamos si hay servicios programados que serán reemplazados
            int conflictos = DataService.Asignaciones.Count(a => a.IdEmpleado == _empleado.Id && dias.Contains(a.Fecha.Date));

            if (conflictos > 0)
            {
                var result = MessageBox.Show(
                    $"Se van a reemplazar {conflictos} servicio(s) por '{t}'.\n\n¿Desea continuar?",
                    "REEMPLAZO DE TURNO",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No) return;
            }

            foreach (var f in dias)
            {
                var tE = DataService.Turnos.FirstOrDefault(x => x.Nombre == t) ??
                         new Turno { Nombre = t, HoraInicio = new TimeSpan(0, 0, 0), HoraFin = new TimeSpan(23, 59, 59) };

                if (tE.Id == 0) DataService.AgregarTurno(tE);

                DataService.Asignaciones.RemoveAll(a => a.IdEmpleado == _empleado.Id && a.Fecha.Date == f.Date);

                DataService.CrearAsignacion(new Asignacion
                {
                    IdEmpleado = _empleado.Id,
                    IdUbicacion = 0,
                    IdTurno = tE.Id,
                    Fecha = f,
                    Estatus = t
                });
            }

            DataService.GuardarTodo();
            lblStatus.Text = $"LOG: ASIGNADO {t} CORRECTAMENTE";
            lblStatus.Foreground = Brushes.LightGray;

            LimpiarSeleccionVisual();
            ActualizarVista();
        }

        private void Celda_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragging = true; _startCell = sender as Border; Mouse.Capture(GridCalendario, CaptureMode.SubTree);
            LimpiarSeleccionVisual();
            if (_startCell != null) _fechasSeleccionadas.Add((DateTime)_startCell.Tag);
        }

        private void Celda_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false; Mouse.Capture(null);
            if (_fechasSeleccionadas.Count > 0)
            {
                _menuContexto.Placement = PlacementMode.MousePoint;
                _menuContexto.IsOpen = true;
            }
        }

        private void LimpiarSeleccionVisual()
        {
            foreach (var c in GridCalendario.Children.OfType<Border>().Where(x => x.Tag is DateTime))
                c.Background = Brushes.Transparent;
            _celdasSeleccionadasVisual.Clear(); _fechasSeleccionadas.Clear();
        }

        private void ActualizarVista()
        {
            if (lblRangoSemana != null) lblRangoSemana.Text = $"{_lunesActual:dd 'de' MMMM} - {_lunesActual.AddDays(6):dd 'de' MMMM yyyy}".ToUpper();
            CargarAsignaciones();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
        private void BtnAsignarHorario_Click(object sender, RoutedEventArgs e) { Mapa win = new Mapa(_empleado.Id); win.Owner = Window.GetWindow(this); if (win.ShowDialog() == true) ActualizarVista(); LimpiarSeleccionVisual(); }
        private void BtnSemanaAtras_Click(object sender, RoutedEventArgs e) { _lunesActual = _lunesActual.AddDays(-7); DibujarGrid(); ActualizarVista(); }
        private void BtnSemanaAdelante_Click(object sender, RoutedEventArgs e) { _lunesActual = _lunesActual.AddDays(7); DibujarGrid(); ActualizarVista(); }
        private void DpSaltoFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpSaltoFecha?.SelectedDate != null)
            {
                DateTime f = dpSaltoFecha.SelectedDate.Value;
                _lunesActual = f.AddDays(-(int)(f.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)f.DayOfWeek - 1));
                DibujarGrid(); ActualizarVista();
            }
        }
    }
}