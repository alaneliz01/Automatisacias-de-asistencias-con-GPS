using Microsoft.Web.WebView2.Core;
using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Secorvi
{
    public partial class Mapa : Page
    {
        private double _selectedLat = 0, _selectedLng = 0;
        private int _idEmpleado;
        private int _idUbicacionSeleccionada = 0; // Para guardar el ID de la tabla ubicaciones
        private List<DateTime> _fechasDestino;

        public Mapa(int idEmpleado, List<DateTime> fechas)
        {
            InitializeComponent();
            _idEmpleado = idEmpleado;
            _fechasDestino = fechas;

            ConfigurarDropdownsHoras();
            RefrescarListaUbicaciones();
            _ = InitMap();

            // 1. CONFIGURAR EL CLICK DERECHO PARA LA LISTA DE UBICACIONES
            ConfigurarMenuContextualUbicaciones();
        }

        private void ConfigurarMenuContextualUbicaciones()
        {
            var menuContextual = new ContextMenu();
            var menuEliminar = new MenuItem { Header = "Eliminar Ubicación" };
            menuEliminar.Click += MenuEliminar_Click;
            menuContextual.Items.Add(menuEliminar);

            lstUbicaciones.ContextMenu = menuContextual;
        }

        private void MenuEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (lstUbicaciones.SelectedItem is Ubicacion u)
            {
                var resultado = MessageBox.Show($"¿Estás seguro de que deseas eliminar la ubicación '{u.nombre_lugar}'?",
                                                "Confirmar Eliminación",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        // IMPORTANTE: Debes crear este método en tu clase DataService
                        // Que ejecute el DELETE FROM Ubicaciones WHERE id = u.id_ubicacion
                        DataService.EliminarUbicacion(u.id_ubicacion);

                        MessageBox.Show("Ubicación eliminada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Limpiamos selección si es la que estaba activa
                        if (_idUbicacionSeleccionada == u.id_ubicacion)
                        {
                            _idUbicacionSeleccionada = 0;
                            txtNombrePunto.Text = "";
                        }

                        RefrescarListaUbicaciones();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al eliminar la ubicación: " + ex.Message, "Error");
                    }
                }
            }
        }

        private void ConfigurarDropdownsHoras()
        {
            var listaHoras = new List<string>();
            for (int i = 1; i <= 12; i++)
            {
                listaHoras.Add($"{i:D2}:00");
                listaHoras.Add($"{i:D2}:30");
            }
            cbHoraInicio.ItemsSource = listaHoras;
            cbHoraFin.ItemsSource = listaHoras;

            cbHoraInicio.Text = "08:00";
            cbAmPmInicio.SelectedIndex = 0;
            cbHoraFin.Text = "04:00";
            cbAmPmFin.SelectedIndex = 1;
        }

        private void RefrescarListaUbicaciones()
        {
            lstUbicaciones.ItemsSource = DataService.Ubicaciones.OrderBy(u => u.nombre_lugar).ToList();
        }

        // --- APARTADO DE MAPA ---
        private async Task InitMap()
        {
            string cache = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Secorvi", "EBWebView");
            var env = await CoreWebView2Environment.CreateAsync(null, cache);
            await mapaWebView.EnsureCoreWebView2Async(env);

            string html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <link rel='stylesheet' href='https://unpkg.com/leaflet-control-geocoder/dist/Control.Geocoder.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <script src='https://unpkg.com/leaflet-control-geocoder/dist/Control.Geocoder.js'></script>
    <style>
        body { margin:0; background:#0B0D12; } 
        #map { height:100vh; width:100vw; } 
        .leaflet-tile-pane { filter: invert(100%) hue-rotate(180deg) brightness(95%) contrast(90%); }
        
        .leaflet-control-geocoder { 
            background: white !important; 
            border: 1px solid #ccc !important; 
            border-radius: 4px !important;
            width: 34px; height: 34px;
            transition: width 0.2s ease;
        }
        .leaflet-control-geocoder-icon { 
            width: 34px !important; height: 34px !important; 
            background-size: 18px 18px !important; 
        }
        .leaflet-control-geocoder-expanded { 
            width: 300px !important; height: 34px !important; 
        }
        .leaflet-control-geocoder-form input { 
            font-size: 14px !important;
            color: #000 !important;
            background: white !important; 
            height: 30px !important;
            border: none !important;
            padding-left: 8px !important;
        }

        .leaflet-control-geocoder-alternatives {
            background: white !important;
            color: black !important;
            font-family: 'Segoe UI', Arial, sans-serif;
            font-size: 13px;
            border: 1px solid #ccc !important;
            border-radius: 0 0 4px 4px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.3);
            max-width: 300px;
        }
        .leaflet-control-geocoder-alternatives li {
            border-bottom: 1px solid #eee;
            padding: 8px 12px !important;
        }
        .leaflet-control-geocoder-alternatives li:hover {
            background: #f0f0f0 !important;
            color: #0078A8 !important;
        }
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        const map = L.map('map', { zoomControl: false }).setView([25.68, -100.31], 12); 
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);
        L.control.zoom({ position: 'topright' }).addTo(map);
        
        let marker;
        let circle; 

        function updatePoint(lat, lng, name, move){ 
            if(marker) map.removeLayer(marker); 
            if(circle) map.removeLayer(circle);

            marker = L.marker([lat, lng]).addTo(map); 

            circle = L.circle([lat, lng], {
                color: '#00F5FF',      
                fillColor: '#00F5FF',  
                fillOpacity: 0.2,      
                radius: 200            
            }).addTo(map);

            if(move) map.setView([lat, lng], 16); 
            window.chrome.webview.postMessage({lat:lat, lng:lng, name: name || ''}); 
        }

        const geocoder = L.Control.geocoder({
            defaultMarkGeocode: false,
            placeholder: 'Buscar dirección...',
            position: 'topright'
        })
        .on('markgeocode', function(e) {
            var center = e.geocode.center;
            updatePoint(center.lat, center.lng, e.geocode.name, true);
        })
        .addTo(map);

        map.on('click', (e) => updatePoint(e.latlng.lat, e.latlng.lng, '', false)); 
        window.updatePos = (lat, lng) => updatePoint(lat, lng, '', true);
    </script>
</body>
</html>";

            mapaWebView.NavigateToString(html);
            mapaWebView.WebMessageReceived += (s, e) => {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                _selectedLat = doc.RootElement.GetProperty("lat").GetDouble();
                _selectedLng = doc.RootElement.GetProperty("lng").GetDouble();

                // Al tocar el mapa, reiniciamos el ID para saber que es un punto nuevo
                _idUbicacionSeleccionada = 0;

                if (doc.RootElement.TryGetProperty("name", out var nameProp))
                {
                    string name = nameProp.GetString();
                    if (!string.IsNullOrEmpty(name)) txtNombrePunto.Text = name.ToUpper();
                }

                txtCoords.Text = string.Format(CultureInfo.InvariantCulture, "{0:F6}, {1:F6}", _selectedLat, _selectedLng);
            };
        }

        private void lstUbicaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUbicaciones.SelectedItem is Ubicacion u)
            {
                txtNombrePunto.Text = u.nombre_lugar;
                _idUbicacionSeleccionada = u.id_ubicacion; // Guardamos el ID real de la base de datos
                _selectedLat = (double)u.latitud;
                _selectedLng = (double)u.longitud;
                txtCoords.Text = string.Format(CultureInfo.InvariantCulture, "{0:F6}, {1:F6}", _selectedLat, _selectedLng);
                mapaWebView.ExecuteScriptAsync($"window.updatePos({_selectedLat.ToString(CultureInfo.InvariantCulture)}, {_selectedLng.ToString(CultureInfo.InvariantCulture)})");
            }
        }

        // --- LÓGICA DE ASIGNACIÓN ---

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_idUbicacionSeleccionada == 0 && (_selectedLat == 0 || _selectedLng == 0))
                {
                    MessageBox.Show("Por favor, selecciona una ubicación de la lista o marca un punto en el mapa.", "Aviso");
                    return;
                }

                string descripcion = txtNombrePunto.Text.Trim();

                // 2. LÓGICA PARA GUARDAR LA NUEVA UBICACIÓN
                if (_idUbicacionSeleccionada == 0 && _selectedLat != 0 && _selectedLng != 0)
                {
                    var nuevaUbicacion = new Ubicacion
                    {
                        nombre_lugar = string.IsNullOrEmpty(descripcion) ? "NUEVO PUNTO MAPA" : descripcion,
                        // Convierte a decimal si en tu modelo 'Ubicacion' está como decimal. Si está como double, quita el cast.
                        latitud = (decimal)_selectedLat,
                        longitud = (decimal)_selectedLng
                    };

                    _idUbicacionSeleccionada = DataService.CrearUbicacionRetornandoId(nuevaUbicacion);

                    // Refrescamos para que ya salga en la lista para la próxima
                    RefrescarListaUbicaciones();
                }

                TimeSpan inicio = GetTimeSpanFromPickers(cbHoraInicio, cbAmPmInicio);
                TimeSpan fin = GetTimeSpanFromPickers(cbHoraFin, cbAmPmFin);

                foreach (var fecha in _fechasDestino)
                {
                    DataService.EliminarAsignacionPorFecha(_idEmpleado, fecha);

                    var nuevaAsig = new Asignacion
                    {
                        id_empleado = _idEmpleado,
                        id_ubicacion = _idUbicacionSeleccionada, // Ahora siempre tendrá un ID válido
                        fecha = fecha,
                        hora_inicio = inicio,
                        hora_fin = fin,
                        descripcion_del_turno = string.IsNullOrEmpty(descripcion) ? "TURNO" : descripcion,
                        estatus = "ASIGNADO"
                    };

                    DataService.CrearAsignacion(nuevaAsig);
                }

                MessageBox.Show($"¡Éxito! Se han asignado {_fechasDestino.Count} días correctamente.", "Secorvi System");
                this.NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al masificar la asignación: " + ex.Message, "Error");
            }
        }

        private TimeSpan GetTimeSpanFromPickers(ComboBox cbHora, ComboBox cbAmPm)
        {
            if (string.IsNullOrEmpty(cbHora.Text)) return TimeSpan.Zero;
            string[] partes = cbHora.Text.Split(':');
            int horas = int.Parse(partes[0]);
            int minutos = int.Parse(partes[1]);
            string amPm = (cbAmPm.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (amPm == "PM" && horas < 12) horas += 12;
            if (amPm == "AM" && horas == 12) horas = 0;

            return new TimeSpan(horas, minutos, 0);
        }

        private void SetPickersFromTimeSpan(TimeSpan ts, ComboBox cbHora, ComboBox cbAmPm)
        {
            int h = ts.Hours;
            string amPm = "AM";
            if (h >= 12) { amPm = "PM"; if (h > 12) h -= 12; }
            if (h == 0) h = 12;
            cbHora.Text = $"{h:D2}:{ts.Minutes:D2}";
            cbAmPm.SelectedIndex = (amPm == "PM") ? 1 : 0;
        }

        private void BtnTurno8_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(8);
        private void BtnTurno12_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(12);
        private void BtnTurno24_Click(object sender, RoutedEventArgs e)
        {
            cbHoraInicio.Text = "12:00";
            cbAmPmInicio.SelectedIndex = 0;
            cbHoraFin.Text = "12:00";
            cbAmPmFin.SelectedIndex = 0;
            txtNombrePunto.Text = "24 HORAS";
        }

        private void AplicarPreajuste(int h)
        {
            TimeSpan inicio = GetTimeSpanFromPickers(cbHoraInicio, cbAmPmInicio);
            TimeSpan fin = inicio.Add(TimeSpan.FromHours(h));
            if (fin.Days >= 1) fin = fin.Subtract(TimeSpan.FromDays(1));
            SetPickersFromTimeSpan(fin, cbHoraFin, cbAmPmFin);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.NavigationService?.GoBack();
    }
}