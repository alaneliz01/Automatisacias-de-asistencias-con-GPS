using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Secorvi
{
    public partial class CalendarioEmpleado : Page
    {
        private Empleado _empleado;
        private DateTime _lunesActual;
        private List<DateTime> _fechasSeleccionadas = new List<DateTime>();
        private bool _isDragging = false;
        private ContextMenu _menuContexto;

        private static readonly Brush ColorSeleccion = new SolidColorBrush(Color.FromArgb(80, 0, 245, 255));

        public CalendarioEmpleado(Empleado emp)
        {
            InitializeComponent();
            _empleado = emp;
            _lunesActual = GetMonday(DateTime.Now);

            // 1. REVISA TU CLASE "Empleado.cs" Y CAMBIA "TU_VARIABLE_AQUI" POR EL NOMBRE CORRECTO.
            // Ejemplos comunes: _empleado?.nombres, _empleado?.NombreCompleto, _empleado?.nombre_empleado
            lblNombreEmpleado.Text = _empleado?.nombre_completo?.ToUpper() ?? "EMPLEADO_NO_IDENTIFICADO";

            // 2. ASÍ SE ASIGNA LA FECHA DE HOY AL CALENDARIO PEQUEÑO (Corregido)
            dpSaltoFecha.SelectedDate = DateTime.Now;

            CrearMenuContexto();

            this.Loaded += async (s, e) =>
            {
                DibujarGrid();
                await ActualizarVistaAsync();
            };
        }

        private DateTime GetMonday(DateTime date) =>
            date.Date.AddDays(-(int)(date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1));

        private void DibujarGrid()
        {
            GridCalendario.RowDefinitions.Clear();
            GridCalendario.ColumnDefinitions.Clear();
            GridCalendario.Children.Clear();

            for (int i = 0; i < 7; i++) GridCalendario.ColumnDefinitions.Add(new ColumnDefinition());

            GridCalendario.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            GridCalendario.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int d = 0; d < 7; d++)
            {
                DateTime fDia = _lunesActual.AddDays(d);

                Border h = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(18, 20, 27)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(35, 40, 50)),
                    BorderThickness = new Thickness(0, 0, 1, 2)
                };

                StackPanel spHeader = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                spHeader.Children.Add(new TextBlock
                {
                    Text = fDia.ToString("dddd", new CultureInfo("es-MX")).ToUpper(),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 110, 130)),
                    FontSize = 9,
                    FontWeight = FontWeights.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spHeader.Children.Add(new TextBlock
                {
                    Text = fDia.ToString("dd MMM").ToUpper(),
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                h.Child = spHeader;
                Grid.SetRow(h, 0); Grid.SetColumn(h, d); GridCalendario.Children.Add(h);

                Border c = new Border
                {
                    Background = Brushes.Transparent,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(35, 40, 50)),
                    BorderThickness = new Thickness(0, 0, (d == 6 ? 0 : 1), 0),
                    Tag = fDia.Date,
                    Uid = "CELL_BASE"
                };

                c.MouseDown += (s, e) => {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        _isDragging = true;
                        LimpiarSeleccionVisual();
                        ToggleSeleccion(s as Border);
                    }
                };
                c.MouseEnter += (s, e) => { if (_isDragging) ToggleSeleccion(s as Border); };
                c.MouseUp += (s, e) => {
                    _isDragging = false;
                    if (_fechasSeleccionadas.Count > 0 && e.ChangedButton == MouseButton.Left)
                        _menuContexto.IsOpen = true;
                };

                Grid.SetRow(c, 1); Grid.SetColumn(c, d); GridCalendario.Children.Add(c);
            }
        }

        private void ToggleSeleccion(Border b)
        {
            DateTime f = (DateTime)b.Tag;
            if (!_fechasSeleccionadas.Contains(f))
            {
                _fechasSeleccionadas.Add(f);
                b.Background = ColorSeleccion;
            }
        }

        private void LimpiarSeleccionVisual()
        {
            _fechasSeleccionadas.Clear();
            foreach (var b in GridCalendario.Children.OfType<Border>().Where(x => x.Uid == "CELL_BASE"))
                b.Background = Brushes.Transparent;
        }

        public async Task ActualizarVistaAsync()
        {
            var visuales = GridCalendario.Children.OfType<FrameworkElement>()
                .Where(b => b.Tag?.ToString() == "VISUAL_ASIG").ToList(); // <-- El ToList() aquí es vital

            foreach (var vis in visuales)
            {
                GridCalendario.Children.Remove(vis);
            }
            await Task.Run(() => DataService.ActualizarTodo());
            var asignaciones = DataService.Asignaciones
                .Where(a => a.id_empleado == _empleado.id_empleado &&
                            a.fecha >= _lunesActual &&
                            a.fecha <= _lunesActual.AddDays(6)).ToList();

            int serviciosTotal = 0, descansosTotal = 0, vacacionesTotal = 0;
            foreach (var asig in asignaciones)
                RenderAsignacion(asig, ref serviciosTotal, ref descansosTotal, ref vacacionesTotal);

            lblTotalServicios.Text = serviciosTotal.ToString();
            lblTotalDescansos.Text = descansosTotal.ToString();
            lblTotalVacaciones.Text = vacacionesTotal.ToString();

            if (lblRangoSemana != null)
                lblRangoSemana.Text = $"{_lunesActual:dd MMM} - {_lunesActual.AddDays(6):dd MMM}".ToUpper();

            // Cambiado a formato 12 horas con AM/PM (hh:mm:ss tt)
            lblFechaHoraActual.Text = $"SINCRO_DATA: {DateTime.Now:hh:mm:ss tt}";
        }

        private void RenderAsignacion(Asignacion asig, ref int s, ref int d, ref int v)
        {
            int col = (int)asig.fecha.DayOfWeek;
            col = (col == 0) ? 6 : col - 1;
            string est = asig.estatus?.ToUpper() ?? "";

            Brush back = Brushes.DarkGray; // Color por defecto
            DateTime dtInicio = DateTime.Today.Add(asig.hora_inicio);
            DateTime dtFin = DateTime.Today.Add(asig.hora_fin);
            string txt = $"{dtInicio:hh:mm tt} - {dtFin:hh:mm tt}";

            // --- SOLUCIÓN AL ERROR DE HILOS ---
            string tit = "SERVICIO";
            try
            {
                // Intentamos leer la ubicación sin .ToList() para evitar el error de array.
                var ubicacion = DataService.Ubicaciones.FirstOrDefault(u => u.id_ubicacion == asig.id_ubicacion);
                if (ubicacion != null)
                {
                    tit = ubicacion.nombre_lugar;
                }
            }
            catch
            {
                // Si hay un choque de hilos con DataService en este exacto milisegundo, 
                // evitamos que la app explote y le asignamos un título genérico.
                tit = "TURNO ASIGNADO";
            }

            // --- LÓGICA DE ESTATUS ---
            if (est == "DESCANSO")
            {
                back = new SolidColorBrush(Color.FromRgb(50, 55, 65)); // Gris
                d++; tit = "DESCANSO"; txt = "LIBRE";
            }
            else if (est == "VACACIONES")
            {
                back = Brushes.DeepSkyBlue; // Azul
                v++; tit = "VACACIONES"; txt = "FULL DAY";
            }
            else if (est == "FALTA")
            {
                back = Brushes.DarkRed; // Rojo Oscuro
                tit = "FALTA"; txt = "NO ASISTIÓ";
            }
            else if (est == "RETRASO")
            {
                back = Brushes.DarkOrange; // Naranja
                tit = "RETRASO"; s++;
            }
            else if (est == "ASISTENCIA")
            {
                back = Brushes.SeaGreen; // Verde
                tit = "ASISTENCIA"; s++;
            }
            else // CAEN "PROGRAMADO" Y LOS REGISTROS VIEJOS QUE DICEN "ASIGNADO"
            {
                back = Brushes.Red; // ROJO
                tit = "PROGRAMADO"; // Forzamos visualmente a que diga PROGRAMADO aunque en BD diga Asignado
                s++;
            }

            Border b = new Border { Background = back, CornerRadius = new CornerRadius(6), Margin = new Thickness(8), Padding = new Thickness(10), Tag = "VISUAL_ASIG", IsHitTestVisible = false };
            StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = tit.ToUpper(), Foreground = Brushes.White, FontWeight = FontWeights.Black, FontSize = 12, TextAlignment = TextAlignment.Center });
            sp.Children.Add(new TextBlock { Text = txt, Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });

            b.Child = sp;
            Grid.SetRow(b, 1); Grid.SetColumn(b, col);
            GridCalendario.Children.Add(b);
        }
        private void CrearMenuContexto()
        {
            _menuContexto = new ContextMenu();
            var m1 = new MenuItem { Header = "➕ ASIGNAR TURNO" };
            m1.Click += (s, e) => BtnAsignarHorario_Click(s, e);

            var m2 = new MenuItem { Header = "💤 MARCAR DESCANSO" };
            m2.Click += async (s, e) => await AsignarEstado("DESCANSO");

            var m3 = new MenuItem { Header = "🛡️ MARCAR VACACIONES" };
            m3.Click += async (s, e) => await AsignarEstado("VACACIONES");

            var m4 = new MenuItem { Header = "🗑️ ELIMINAR ASIGNACIÓN" };
            m4.Click += async (s, e) => {
                foreach (var f in _fechasSeleccionadas)
                {
                    var a = DataService.Asignaciones.FirstOrDefault(x => x.id_empleado == _empleado.id_empleado && x.fecha.Date == f.Date);
                    if (a != null) DataService.EliminarAsignacion(a.id_asignacion);
                }
                LimpiarSeleccionVisual();
                await ActualizarVistaAsync();
            };

            _menuContexto.Items.Add(m1); _menuContexto.Items.Add(m2); _menuContexto.Items.Add(m3); _menuContexto.Items.Add(new Separator()); _menuContexto.Items.Add(m4);
        }

        private async Task AsignarEstado(string est)
        {
            foreach (var f in _fechasSeleccionadas)
            {
                var ex = DataService.Asignaciones.FirstOrDefault(x => x.id_empleado == _empleado.id_empleado && x.fecha.Date == f.Date);
                if (ex != null) DataService.EliminarAsignacion(ex.id_asignacion);
                DataService.CrearAsignacion(new Asignacion { id_empleado = _empleado.id_empleado, fecha = f, estatus = est, descripcion_del_turno = est, hora_inicio = new TimeSpan(0, 0, 0), hora_fin = new TimeSpan(23, 59, 59) });
            }
            LimpiarSeleccionVisual();
            await ActualizarVistaAsync();
        }

        private void BtnAsignarHorario_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validación de seguridad
            if (_fechasSeleccionadas == null || _fechasSeleccionadas.Count == 0)
            {
                MessageBox.Show("Por favor, selecciona al menos un día en el calendario.",
                                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Ordenamos las fechas para que la lógica sea consistente (opcional pero recomendado)
            var fechasParaAsignar = _fechasSeleccionadas.OrderBy(f => f).ToList();

            // 3. Navegamos pasando la lista completa
            // Nota: Deberás ajustar el constructor de la clase Mapa para recibir List<DateTime>
            Mapa paginaMapa = new Mapa(_empleado.id_empleado, fechasParaAsignar);
            this.NavigationService?.Navigate(paginaMapa);

            // 4. Limpiamos la selección después de enviar los datos
            LimpiarSeleccionVisual();
        }

        private async void BtnSemanaAtras_Click(object sender, RoutedEventArgs e)
        {
            _lunesActual = _lunesActual.AddDays(-7);
            DibujarGrid();
            await ActualizarVistaAsync();
        }

        private async void BtnSemanaAdelante_Click(object sender, RoutedEventArgs e)
        {
            _lunesActual = _lunesActual.AddDays(7);
            DibujarGrid();
            await ActualizarVistaAsync();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e) =>
            this.NavigationService?.GoBack();

        private async void DpSaltoFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpSaltoFecha.SelectedDate.HasValue)
            {
                _lunesActual = GetMonday(dpSaltoFecha.SelectedDate.Value);
                DibujarGrid();
                await ActualizarVistaAsync();
            }
        }
    }
}