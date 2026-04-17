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

            var emp = DataService.Empleados.FirstOrDefault(e => e.Id == _idEmpleadoPreseleccionado);
            if (lblEmpleadoActivo != null)
                lblEmpleadoActivo.Text = $"AGENTE: {(emp?.Nombre ?? "---").ToUpper()}";

            this.Loaded += async (s, e) => {
                await InitMap();
                RefrescarListaUbicaciones();
            };
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
        
        /* FILTRO OSCURO TOTAL */
        .leaflet-tile { filter: brightness(0.6) invert(1) contrast(3) hue-rotate(200deg) saturate(0.3) !important; }

        /* CONTENEDOR DEL BUSCADOR */
        #search-panel {
            position: absolute; top: 20px; right: 20px; z-index: 1000; width: 350px;
        }

        .search-bar {
            position: relative; background: #1E222D; border: 2px solid #FFB300; border-radius: 12px;
            padding: 4px 15px; box-shadow: 0 6px 20px rgba(0,0,0,0.6);
        }

        .search-bar input {
            width: 100%; background: transparent; border: none; color: white;
            padding: 12px 0; outline: none; font-size: 15px;
        }

        /* SUGERENCIAS NATIVAS MEJORADAS */
        #suggestions-list {
            background: #1E222D; border: 2px solid #303645; border-radius: 12px;
            margin-top: 10px; max-height: 300px; overflow-y: auto; display: none;
            box-shadow: 0 12px 30px rgba(0,0,0,0.8);
        }

        .suggestion-item {
            padding: 14px 16px; border-bottom: 1px solid #2A2E3B; color: #E5E7EB;
            cursor: pointer; font-size: 14px; transition: all 0.2s;
        }

        .suggestion-item:hover, .suggestion-item.active {
            background: #FFB300; color: #000;
        }

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

        // LÓGICA DE BÚSQUEDA
        searchInput.oninput = (e) => {
            clearTimeout(debounceTimer);
            const query = e.target.value;
            if (query.length < 3) { list.style.display = 'none'; return; }
            
            debounceTimer = setTimeout(async () => {
                try {
                    const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=6&countrycodes=mx`);
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
                    
                    // Resaltar texto buscado
                    const regex = new RegExp(`(${query})`, 'gi');
                    div.innerHTML = item.display_name.replace(regex, ""<span class='highlight'>$1</span>"");
                    
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

        // NAVEGACIÓN CON TECLADO
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
                            txtCoords.Text = $"{_selectedLat:F6}, {_selectedLng:F6}";
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
            lstUbicaciones.ItemsSource = DataService.Ubicaciones.OrderBy(u => u.Nombre).ToList();
        }

        private void lstUbicaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUbicaciones.SelectedItem is Ubicacion u)
            {
                txtNombrePunto.Text = u.Nombre;
                _selectedLat = u.Latitud; _selectedLng = u.Longitud;
                txtCoords.Text = $"{_selectedLat:F6}, {_selectedLng:F6}";
                mapaWebView.ExecuteScriptAsync($"window.updatePos({_selectedLat.ToString(CultureInfo.InvariantCulture)}, {_selectedLng.ToString(CultureInfo.InvariantCulture)})");
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLat == 0 || string.IsNullOrWhiteSpace(txtHoraInicio.Text))
            {
                MessageBox.Show("Seleccione un punto."); return;
            }
            try
            {
                DateTime dtI = DateTime.ParseExact(txtHoraInicio.Text.Trim(), "hh:mm tt", CultureInfo.InvariantCulture);
                DateTime dtF = DateTime.ParseExact(txtHoraFin.Text.Trim(), "hh:mm tt", CultureInfo.InvariantCulture);
                Ubicacion ubi = DataService.Ubicaciones.FirstOrDefault(u => u.Nombre == txtNombrePunto.Text.ToUpper()) ??
                               new Ubicacion { Nombre = txtNombrePunto.Text.ToUpper(), Latitud = _selectedLat, Longitud = _selectedLng, Activo = true, RadioPermitido = 200 };

                if (ubi.Id == 0) { DataService.AgregarUbicacion(ubi); DataService.CargarUbicaciones(); ubi = DataService.Ubicaciones.Last(); }

                var t = DataService.Turnos.FirstOrDefault(x => x.HoraInicio == dtI.TimeOfDay && x.HoraFin == dtF.TimeOfDay) ??
                        new Turno { Nombre = "PERSONALIZADO", HoraInicio = dtI.TimeOfDay, HoraFin = dtF.TimeOfDay };

                if (t.Id == 0) { DataService.AgregarTurno(t); DataService.CargarTurnos(); t = DataService.Turnos.Last(); }

                DataService.EliminarAsignacionPorFecha(_idEmpleadoPreseleccionado, dpFecha.SelectedDate ?? DateTime.Today);
                DataService.CrearAsignacion(new Asignacion
                {
                    IdEmpleado = _idEmpleadoPreseleccionado,
                    IdUbicacion = ubi.Id,
                    IdTurno = t.Id,
                    Fecha = dpFecha.SelectedDate ?? DateTime.Today,
                    Estatus = "PROGRAMADO"
                });
                this.DialogResult = true; this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnEliminarVarios_Click(object sender, RoutedEventArgs e)
        {
            var items = lstUbicaciones.SelectedItems.Cast<Ubicacion>().ToList();

            if (items.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una ubicación para eliminar.");
                return;
            }
            var confirm = MessageBox.Show($"¿Desea eliminar {items.Count} ubicaciones permanentemente?",
                                         "CONFIRMAR ELIMINACIÓN", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    DataService.EliminarUbicaciones(items);
                    RefrescarListaUbicaciones();
                    MessageBox.Show("Ubicaciones eliminadas de la base de datos.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message);
                }
            }
        }
        private void BtnCancelarSeleccion_Click(object sender, RoutedEventArgs e) => lstUbicaciones.UnselectAll();
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}