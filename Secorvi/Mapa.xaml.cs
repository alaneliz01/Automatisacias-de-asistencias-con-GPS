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

// esto es la página del mapa, la cual se encarga de mostrar el mapa interactivo y permitir asignar puntos de servicio a los empleados. Utiliza WebView2 para mostrar un mapa de Leaflet con OpenStreetMap, y permite buscar direcciones, seleccionar puntos en el mapa y guardar las asignaciones. También actualiza la nómina del empleado según el turno asignado.
namespace Secorvi
{
    public partial class Mapa : Page
    {
        private double _selectedLat = 0;
        private double _selectedLng = 0;
        private double rangoActual = 0.2;

        public Mapa()
        {
            InitializeComponent();
            lstEmpleados.ItemsSource = DataService.CargarEmpleados();
            this.Loaded += async (s, e) => await InitMap();

            lstEmpleados.SelectionChanged += (s, e) => {
                if (lstEmpleados.SelectedItem is Empleado emp)
                {
                    lblEmpleadoActivo.Text = $"AGENTE: {emp.Nombre}";
                    txtNombrePunto.Text = emp.PuntoServicio ?? "";

                    var asig = DataService.Asignaciones.FirstOrDefault(a => a.EmpleadoId == emp.Id);
                    if (asig != null)
                    {
                        var ubi = DataService.Ubicaciones.FirstOrDefault(u => u.Id == asig.UbicacionId);
                        if (ubi != null)
                        {
                            _selectedLat = ubi.Latitud;
                            _selectedLng = ubi.Longitud;
                            rangoActual = ubi.RadioCerca > 0 ? ubi.RadioCerca : 0.2;
                            ActualizarMapaJS(true);
                        }
                    }
                }
            };
        }

        private void ActualizarMapaJS(bool move)
        {
            if (mapaWebView?.CoreWebView2 == null || _selectedLat == 0) return;
            string lat = _selectedLat.ToString(CultureInfo.InvariantCulture);
            string lng = _selectedLng.ToString(CultureInfo.InvariantCulture);
            string rad = rangoActual.ToString(CultureInfo.InvariantCulture);
            mapaWebView.ExecuteScriptAsync($"window.updateVisuals({lat}, {lng}, {rad}, {(move ? "true" : "false")})");
        }

        private async Task InitMap()
        {
            await mapaWebView.EnsureCoreWebView2Async();
            mapaWebView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 SecorviApp/1.0";

            mapaWebView.WebMessageReceived += (s, e) => {
                try
                {
                    var json = JsonDocument.Parse(e.WebMessageAsJson);
                    _selectedLat = json.RootElement.GetProperty("lat").GetDouble();
                    _selectedLng = json.RootElement.GetProperty("lng").GetDouble();
                    Dispatcher.Invoke(() => {
                        txtCoords.Text = $"COORD: {_selectedLat:F4}, {_selectedLng:F4}";
                        ActualizarMapaJS(false);
                    });
                }
                catch { }
            };
            // NO MOVER  ESTO ES EL MAPA!!!
            string html = @"<!DOCTYPE html><html><head><meta charset='utf-8'/>
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <style>
                    @import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;700&display=swap');
                    body{margin:0; padding:0; font-family: 'Roboto', sans-serif;} #map{height:100vh; width:100vw;}
                    .search-box{
                        position:absolute; top:20px; right:20px; z-index:1000; 
                        background:#ffffff; border-radius:2px; border:1px solid #ccc;
                        width:320px; box-shadow: 0 2px 6px rgba(0,0,0,0.3);
                    }
                    .input-group{ display:flex; background:#fff; }
                    .search-box input{
                        border:none; background:transparent; color:#202124; 
                        padding:12px; outline:none; flex-grow:1; font-size:15px;
                    }
                    .search-box button{background:#fff; border:none; cursor:pointer; padding:0 15px; color:#4285f4; font-size:18px;}
                    #results{
                        background:#fff; max-height:300px; overflow-y:auto; 
                        display:none; border-top:1px solid #eee;
                    }
                    .result-item{
                        padding:12px 15px; color:#3c4043; cursor:pointer; font-size:13px;
                        border-bottom:1px solid #eee; transition: background 0.2s;
                    }
                    .result-item:hover{ background:#f1f3f4; }
                    .result-item b { color: #202124; }
                </style></head>
                <body>
                    <div class='search-box'>
                        <div class='input-group'>
                            <input type='text' id='adr' placeholder='Buscar en Monterrey...' oninput='autoComplete()'>
                            <button onclick='geo()'>🔍</button>
                        </div>
                        <div id='results'></div>
                    </div>
                    <div id='map'></div>
                    <script>
                        const map = L.map('map', {zoomControl: false}).setView([25.6844, -100.3161], 12);
                        L.control.zoom({position: 'bottomright'}).addTo(map);
                        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);
                        let m=null, c=null;
                        const viewbox = '-100.6,25.4,-99.9,25.9'; 

                        window.updateVisuals = (la, lo, ra, mo) => {
                            if(m) map.removeLayer(m); if(c) map.removeLayer(c);
                            m = L.marker([la, lo]).addTo(map);
                            c = L.circle([la, lo], {radius: ra*1000, color:'#4285f4', fillColor:'#4285f4', fillOpacity:0.2, weight:1}).addTo(map);
                            if(mo) map.setView([la, lo], 16);
                        };

                        map.on('click', (e) => { window.chrome.webview.postMessage({lat: e.latlng.lat, lng: e.latlng.lng}); });

                        let timeout = null;
                        async function autoComplete() {
                            const q = document.getElementById('adr').value;
                            const resDiv = document.getElementById('results');
                            if(q.length < 3) { resDiv.style.display = 'none'; return; }
                            clearTimeout(timeout);
                            timeout = setTimeout(async () => {
                                const url = `https://nominatim.openstreetmap.org/search?format=json&q=${q}&viewbox=${viewbox}&bounded=1&limit=5&addressdetails=1`;
                                const r = await fetch(url);
                                const data = await r.json();
                                resDiv.innerHTML = '';
                                if(data.length > 0) {
                                    resDiv.style.display = 'block';
                                    data.forEach(item => {
                                        const div = document.createElement('div');
                                        div.className = 'result-item';
                                        const name = item.display_name.split(',').slice(0,3).join(',');
                                        div.innerHTML = `<b>${name}</b>`;
                                        div.onclick = () => selectItem(item.lat, item.lon, name);
                                        resDiv.appendChild(div);
                                    });
                                }
                            }, 300);
                        }
                        function selectItem(lat, lon, name) {
                            const la = parseFloat(lat), lo = parseFloat(lon);
                            document.getElementById('adr').value = name;
                            document.getElementById('results').style.display = 'none';
                            map.setView([la, lo], 16);
                            window.chrome.webview.postMessage({lat: la, lng: lo});
                        }
                        async function geo() {
                            const q = document.getElementById('adr').value;
                            if(!q) return;
                            const r = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${q}&viewbox=${viewbox}&bounded=1&limit=1`);
                            const d = await r.json();
                            if(d.length > 0) selectItem(d[0].lat, d[0].lon, d[0].display_name);
                        }
                    </script></body></html>";
            mapaWebView.NavigateToString(html);
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lstEmpleados == null) return;
            lstEmpleados.ItemsSource = DataService.CargarEmpleados()
                .Where(x => x.Nombre.ToUpper().Contains(txtBusqueda.Text.ToUpper())).ToList();
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e) => this.NavigationService.GoBack();
        private void BtnRango100_Click(object sender, RoutedEventArgs e) { rangoActual = 0.1; ActualizarMapaJS(false); }
        private void BtnRango200_Click(object sender, RoutedEventArgs e) { rangoActual = 0.2; ActualizarMapaJS(false); }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (lstEmpleados.SelectedItem is not Empleado emp || _selectedLat == 0)
            {
                MessageBox.Show("Seleccione un agente y un punto en el mapa.");
                return;
            }

            // Crear nueva ubicación
            var ubi = new Ubicacion
            {
                Id = DataService.Ubicaciones.Count + 1,
                Nombre = string.IsNullOrWhiteSpace(txtNombrePunto.Text) ? "PUNTO NUEVO" : txtNombrePunto.Text.ToUpper(),
                Latitud = _selectedLat,
                Longitud = _selectedLng,
                RadioCerca = rangoActual
            };
            DataService.Ubicaciones.Add(ubi);

            // Vincular asignación
            DataService.Asignaciones.RemoveAll(a => a.EmpleadoId == emp.Id);
            DataService.Asignaciones.Add(new Asignacion
            {
                Id = DataService.Asignaciones.Count + 1,
                EmpleadoId = emp.Id,
                UbicacionId = ubi.Id
            });

            // REFLEJAR EN NÓMINA 
            if (dpFecha.SelectedDate.HasValue)
            {
                string diaSemana = dpFecha.SelectedDate.Value.ToString("dddd", new CultureInfo("en-US"));
                string tipo = emp.TurnoTipo ?? "12/D";
                string valorTurno = tipo.Contains("12") ? "12" : (tipo.Contains("24") ? "24" : "D");

                switch (diaSemana)
                {
                    case "Monday": emp.Lunes = valorTurno; break;
                    case "Tuesday": emp.Martes = valorTurno; break;
                    case "Wednesday": emp.Miercoles = valorTurno; break;
                    case "Thursday": emp.Jueves = valorTurno; break;
                    case "Friday": emp.Viernes = valorTurno; break;
                    case "Saturday": emp.Sabado = valorTurno; break;
                    case "Sunday": emp.Domingo = valorTurno; break;
                }
            }

            emp.PuntoServicio = ubi.Nombre;

            // CÁLCULO DE SUELDO 
            decimal tarifaPorHora = 35.5m;
            decimal totalHorasSemana = 0;

            string[] diasValores = { emp.Lunes, emp.Martes, emp.Miercoles, emp.Jueves, emp.Viernes, emp.Sabado, emp.Domingo };

            foreach (string valor in diasValores)
            {
                if (decimal.TryParse(valor, out decimal horas))
                {
                    totalHorasSemana += horas;
                }
                else if (valor == "D" || valor == "A")
                {
                    totalHorasSemana += 12;
                }
            }

            emp.TotalSueldo = totalHorasSemana * tarifaPorHora;

            DataService.GuardarTodo();
            MessageBox.Show($"Asignación guardada para {emp.Nombre}. Nómina actualizada.");
            this.NavigationService.GoBack();
        }
    }
}