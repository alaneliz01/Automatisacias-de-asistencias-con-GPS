using Microsoft.Web.WebView2.Core;
using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace Secorvi
{
    public partial class Mapa : Window
    {
        
        private double _selectedLat = 0;
        private double _selectedLng = 0;
        private int _idEmpleadoPreseleccionado;
        private DateTime _fechaAsignacion;


        public Mapa(int idEmpleado = 0, DateTime? fechaAsignacion = null)
        {
            InitializeComponent();
            _idEmpleadoPreseleccionado = idEmpleado;
            _fechaAsignacion = fechaAsignacion ?? DateTime.Today;

            if (dpFecha != null) dpFecha.SelectedDate = _fechaAsignacion;

            var emp = DataService.Empleados.FirstOrDefault(e => e.id_empleado == _idEmpleadoPreseleccionado);
            if (lblEmpleadoActivo != null)
                lblEmpleadoActivo.Text = $"AGENTE: {(emp?.nombre_completo ?? "---").ToUpper()}";
            List<string> horas = new List<string>();
            for (int h = 1; h <= 12; h++)
            {
                horas.Add($"{h:D2}:00");
                horas.Add($"{h:D2}:30");
            }
            cbHoraInicio.ItemsSource = horas;
            cbHoraFin.ItemsSource = horas;
            cbHoraInicio.Text = "07:00";
            cbHoraFin.Text = "03:00";

            this.Loaded += async (s, e) => {
                await InitMap();
                RefrescarListaUbicaciones();
            };
        }

        private void LlenarDropdownsHoras()
        {
            List<string> horas = new List<string>();
            for (int h = 1; h <= 12; h++)
            {
                horas.Add($"{h:D2}:00");
                horas.Add($"{h:D2}:30");
            }
            cbHoraInicio.ItemsSource = horas;
            cbHoraFin.ItemsSource = horas;
            cbHoraInicio.Text = "07:00";
            cbHoraFin.Text = "03:00";
        }

        private async Task InitMap()
        {
            try
            {
                await mapaWebView.EnsureCoreWebView2Async();

                string html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body { margin:0; padding:0; overflow: hidden; background: #0B0D12; font-family: 'Segoe UI', sans-serif; }
        #map { height:100vh; width: 100vw; z-index: 1; }
        .leaflet-tile { filter: brightness(0.6) invert(1) contrast(3) hue-rotate(200deg) saturate(0.3) !important; }
        #search-panel { position: absolute; top: 20px; right: 20px; z-index: 1000; width: 350px; }
        .search-bar { position: relative; background: #1E222D; border: 2px solid #FFB300; border-radius: 12px; padding: 4px 15px; box-shadow: 0 6px 20px rgba(0,0,0,0.6); }
        .search-bar input { width: 100%; background: transparent; border: none; color: white; padding: 12px 0; outline: none; font-size: 15px; }
        #suggestions-list { background: #1E222D; border: 2px solid #303645; border-radius: 12px; margin-top: 10px; max-height: 300px; overflow-y: auto; display: none; box-shadow: 0 12px 30px rgba(0,0,0,0.8); }
        .suggestion-item { padding: 14px 16px; border-bottom: 1px solid #2A2E3B; color: #E5E7EB; cursor: pointer; font-size: 14px; transition: all 0.2s; }
        .suggestion-item:hover, .suggestion-item.active { background: #FFB300; color: #000; }
        .suggestion-item:last-child { border-bottom: none; }
        .highlight { font-weight: bold; color: #FFB300; }
        .suggestion-item:hover .highlight, .suggestion-item.active .highlight { color: #000; }
    </style>
</head>
<body>
    <div id='search-panel'>
        <div class='search-bar'>
            <input type='text' id='searchInput' placeholder='Buscar dirección...' autocomplete='off'>
        </div>
        <div id='suggestions-list'></div>
    </div>
    <div id='map'></div>
    <script>
        const map = L.map('map', { zoomControl: false }).setView([25.6844, -100.3161], 12);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);
        let marker, circle, debounceTimer;
        let activeIndex = -1;
        const searchInput = document.getElementById('searchInput');
        const list = document.getElementById('suggestions-list');

        searchInput.oninput = (e) => {
            clearTimeout(debounceTimer);
            const query = e.target.value;
            if (query.length < 3) { list.style.display = 'none'; return; }
            debounceTimer = setTimeout(async () => {
                try {
                    const response = await fetch('https://nominatim.openstreetmap.org/search?format=json&q=' + encodeURIComponent(query) + '&limit=6&countrycodes=mx');
                    const data = await response.json();
                    renderSuggestions(data, query);
                } catch (err) { console.error(err); }
            }, 300);
        };

        function renderSuggestions(data, query) {
            list.innerHTML = '';
            activeIndex = -1;
            if (data.length > 0) {
                list.style.display = 'block';
                data.forEach((item, index) => {
                    const div = document.createElement('div');
                    div.className = 'suggestion-item';
                    const regex = new RegExp('(' + query + ')', 'gi');
                    div.innerHTML = item.display_name.replace(regex, '<span class=""highlight"">$1</span>');
                    div.onclick = () => selectItem(item);
                    list.appendChild(div);
                });
            } else { list.style.display = 'none'; }
        }

        function selectItem(item) {
            updatePoint(parseFloat(item.lat), parseFloat(item.lon), true);
            searchInput.value = item.display_name;
            list.style.display = 'none';
        }

        searchInput.onkeydown = (e) => {
            const items = list.getElementsByClassName('suggestion-item');
            if (e.key === 'ArrowDown') {
                activeIndex = (activeIndex + 1) % items.length;
                updateActive(items);
            } else if (e.key === 'ArrowUp') {
                activeIndex = (activeIndex - 1 + items.length) % items.length;
                updateActive(items);
            } else if (e.key === 'Enter' && activeIndex > -1) {
                items[activeIndex].click();
            }
        };

        function updateActive(items) {
            for (let i = 0; i < items.length; i++) items[i].classList.remove('active');
            if (activeIndex > -1) items[activeIndex].classList.add('active');
        }

        function updatePoint(lat, lng, moveTo = false) {
            if (marker) map.removeLayer(marker);
            if (circle) map.removeLayer(circle);
            marker = L.marker([lat, lng]).addTo(map);
            circle = L.circle([lat, lng], { color: '#FFB300', fillColor: '#FFB300', fillOpacity: 0.2, radius: 200 }).addTo(map);
            if (moveTo) map.setView([lat, lng], 17);
            window.chrome.webview.postMessage({lat: lat, lng: lng});
        }

        map.on('click', (e) => { updatePoint(e.latlng.lat, e.latlng.lng, false); list.style.display = 'none'; });
        window.updatePos = (lat, lng) => updatePoint(lat, lng, true);
    </script>
</body></html>";

                mapaWebView.NavigateToString(html);
                mapaWebView.WebMessageReceived += (s, e) => {
                    try
                    {
                        using (var doc = JsonDocument.Parse(e.WebMessageAsJson))
                        {
                            _selectedLat = doc.RootElement.GetProperty("lat").GetDouble();
                            _selectedLng = doc.RootElement.GetProperty("lng").GetDouble();
                            txtCoords.Text = string.Format("{0:F6}, {1:F6}", _selectedLat, _selectedLng);
                        }
                    }
                    catch { }
                };
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void RefrescarListaUbicaciones()
        {
            lstUbicaciones.ItemsSource = null;
            lstUbicaciones.ItemsSource = DataService.Ubicaciones.OrderBy(u => u.nombre_lugar).ToList();
        }

        private void lstUbicaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUbicaciones.SelectedItem is Ubicacion u)
            {
                txtNombrePunto.Text = u.nombre_lugar;
                _selectedLat = (double)u.latitud;
                _selectedLng = (double)u.longitud;

                // Usamos CultureInfo.InvariantCulture para asegurar que el punto decimal sea '.' y no ','
                txtCoords.Text = string.Format(CultureInfo.InvariantCulture, "{0:F6}, {1:F6}", _selectedLat, _selectedLng);

                mapaWebView.ExecuteScriptAsync(string.Format(CultureInfo.InvariantCulture,
                    "window.updatePos({0}, {1})", _selectedLat, _selectedLng));
            }
        }

        private void BtnTurno8_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(8);
        private void BtnTurno12_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(12);
        private void BtnTurno24_Click(object sender, RoutedEventArgs e)
        {
            cbHoraInicio.Text = "12:00";
            cbAmPmI.SelectedIndex = 0; 
            cbHoraFin.Text = "12:00";
            cbAmPmF.SelectedIndex = 0;
        }
        private void AplicarPreajuste(int horas)
        {
            try
            {
                string horaStr = cbHoraInicio.Text.Trim();
                string amPm = (cbAmPmI.SelectedItem as ComboBoxItem).Content.ToString();

                // Parseamos la entrada
                DateTime entrada = DateTime.ParseExact($"{horaStr} {amPm}",
                    new[] { "h:mm tt", "hh:mm tt" }, CultureInfo.InvariantCulture, DateTimeStyles.None);

                // Sumamos las horas (8, 12 o 24)
                DateTime salida = entrada.AddHours(horas);

                // Asignamos el texto (Formato 12h)
                cbHoraFin.Text = salida.ToString("hh:mm");

                // Seleccionamos AM (index 0) o PM (index 1) basado en el resultado
                cbAmPmF.SelectedIndex = salida.ToString("tt", CultureInfo.InvariantCulture).ToUpper().Contains("AM") ? 0 : 1;
            }
            catch
            {
                MessageBox.Show("Formato de entrada inválido en Inicio. Use HH:MM (Ej: 07:00)");
            }
        }
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLat == 0 || string.IsNullOrWhiteSpace(cbHoraInicio.Text) || string.IsNullOrWhiteSpace(txtNombrePunto.Text))
            {
                MessageBox.Show("Por favor, complete el nombre del lugar y seleccione un punto en el mapa.");
                return;
            }

            try
            {
                string entradaFinal = $"{cbHoraInicio.Text.Trim()} {(cbAmPmI.SelectedItem as ComboBoxItem).Content}";
                string salidaFinal = $"{cbHoraFin.Text.Trim()} {(cbAmPmF.SelectedItem as ComboBoxItem).Content}";
                string[] formatos = { "h:mm tt", "hh:mm tt" };
                DateTime dtI = DateTime.ParseExact(entradaFinal, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None);
                DateTime dtF = DateTime.ParseExact(salidaFinal, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None);
                string nombrePunto = txtNombrePunto.Text.Trim().ToUpper();
                Ubicacion ubi = DataService.Ubicaciones.FirstOrDefault(u => u.nombre_lugar == nombrePunto);

                if (ubi == null)
                {
                    ubi = new Ubicacion
                    {
                        nombre_lugar = nombrePunto,
                        latitud = (decimal)_selectedLat,
                        longitud = (decimal)_selectedLng,
                        radio_permitido = 200
                    };
                    DataService.AgregarUbicacion(ubi);
                }
                else
                {
                    ubi.latitud = (decimal)_selectedLat;
                    ubi.longitud = (decimal)_selectedLng;
                    DataService.ActualizarUbicacion(ubi);
                }

                DataService.CargarUbicaciones();
                ubi = DataService.Ubicaciones.FirstOrDefault(u => u.nombre_lugar == nombrePunto);
                DateTime fechaDestino = dpFecha.SelectedDate ?? DateTime.Today;

                if (ubi == null || ubi.id_ubicacion <= 0)
                    throw new Exception("Error crítico: No se pudo recuperar el ID de la ubicación.");

                var nuevaAsignacion = new Asignacion
                {
                    id_empleado = _idEmpleadoPreseleccionado,
                    id_ubicacion = ubi.id_ubicacion,
                    descripcion_del_turno = nombrePunto.Length > 16 ? nombrePunto.Substring(0, 16) : nombrePunto,
                    fecha = fechaDestino,
                    hora_inicio = dtI.TimeOfDay,
                    hora_fin = dtF.TimeOfDay,
                    estatus = "PROGRAMADO"
                };

                DataService.CrearAsignacion(nuevaAsignacion);

                MessageBox.Show("Asignación registrada exitosamente.");
                this.DialogResult = true;
                this.Close();
            }
            catch (FormatException)
            {
                MessageBox.Show("Error de formato: Asegúrese de escribir la hora como HH:MM (Ejemplo: 08:30)");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }


        private void BtnEliminarVarios_Click(object sender, RoutedEventArgs e)
        {
            var items = lstUbicaciones.SelectedItems.Cast<Ubicacion>().ToList();
            if (items.Count == 0) return;

            var confirm = MessageBox.Show(string.Format("¿Desea eliminar {0} ubicaciones?", items.Count), "CONFIRMAR", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    DataService.EliminarUbicaciones(items);
                    RefrescarListaUbicaciones();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void BtnCancelarSeleccion_Click(object sender, RoutedEventArgs e) => lstUbicaciones.UnselectAll();
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}