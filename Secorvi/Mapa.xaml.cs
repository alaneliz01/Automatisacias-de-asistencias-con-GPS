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
            lblEmpleadoActivo.Text = emp == null
                ? "🗺️ GESTIÓN DE UBICACIONES"
                : $"👤 ASIGNAR A: {emp.Nombre.ToUpper()}";

            this.Loaded += async (s, e) =>
            {
                await InitMap();
                RefrescarListaUbicaciones();
            };

            if (txtBusqueda != null)
                txtBusqueda.TextChanged += (s, e) =>
                {
                    string bus = txtBusqueda.Text.ToUpper();
                    lstUbicaciones.ItemsSource = DataService.Ubicaciones
                        .Where(u => u.Nombre.ToUpper().Contains(bus))
                        .OrderBy(u => u.Nombre).ToList();
                };
        }

        private void RefrescarListaUbicaciones()
        {
            lstUbicaciones.ItemsSource = null;
            lstUbicaciones.ItemsSource = DataService.Ubicaciones.OrderBy(u => u.Nombre).ToList();
        }

        // --- ESTE ES EL MÉTODO QUE FALTABA ---
        private void ActualizarMapaJS(bool move)
        {
            if (mapaWebView?.CoreWebView2 == null || _selectedLat == 0) return;

            // Usamos InvariantCulture para que los decimales sean con punto (.) y no con coma (,)
            string lat = _selectedLat.ToString(CultureInfo.InvariantCulture);
            string lng = _selectedLng.ToString(CultureInfo.InvariantCulture);

            // Ejecutamos la función que está dentro del HTML del mapa
            mapaWebView.ExecuteScriptAsync($"window.updateVisuals({lat}, {lng}, 200, {(move ? "true" : "false")})");
        }

        private async Task InitMap()
        {
            try
            {
                await mapaWebView.EnsureCoreWebView2Async();
                mapaWebView.WebMessageReceived += (s, e) =>
                {
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(e.WebMessageAsJson))
                        {
                            _selectedLat = doc.RootElement.GetProperty("lat").GetDouble();
                            _selectedLng = doc.RootElement.GetProperty("lng").GetDouble();
                            Dispatcher.Invoke(() =>
                            {
                                txtCoords.Text = $"{_selectedLat:F6}, {_selectedLng:F6}";
                                ActualizarMapaJS(false);
                            });
                        }
                    }
                    catch { /* Ignorar errores de parsing */ }
                };

                string html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body { margin:0; padding:0; background:#0F111A; }
        #map { height:100vh; width:100vw; background:#0F111A; }
        .leaflet-tile { 
            filter: brightness(0.7) invert(0.9) contrast(1.2) hue-rotate(200deg) saturate(0.8) !important; 
        }
        .leaflet-control-zoom { background:#1E252F !important; border:#2D323E 1px solid !important; }
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        const map = L.map('map', { zoomControl: true }).setView([25.6844, -100.3161], 12);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);
        
        let marker = null, circle = null;
        
        window.updateVisuals = function(lat, lng, radius, moveTo) {
            if (marker) map.removeLayer(marker);
            if (circle) map.removeLayer(circle);
            
            marker = L.marker([lat, lng]).addTo(map);
            circle = L.circle([lat, lng], { radius: radius, color: '#FFB300', fillOpacity: 0.25 }).addTo(map);
            
            if (moveTo) map.setView([lat, lng], 16);
        };
        
        map.on('click', function(e) {
            window.chrome.webview.postMessage({ lat: e.latlng.lat, lng: e.latlng.lng });
        });
    </script>
</body></html>";

                mapaWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error WebView2: " + ex.Message);
            }
        }

        private void lstUbicaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUbicaciones.SelectedItem is Ubicacion u)
            {
                txtNombrePunto.Text = u.Nombre;
                _selectedLat = u.Latitud;
                _selectedLng = u.Longitud;
                txtCoords.Text = $"{_selectedLat:F6}, {_selectedLng:F6}";
                ActualizarMapaJS(true);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLat == 0 || string.IsNullOrEmpty(txtNombrePunto.Text))
            {
                MessageBox.Show("Seleccione un punto en el mapa y asigne un nombre.");
                return;
            }

            try
            {
                var ubiMaestra = DataService.Ubicaciones.FirstOrDefault(u => u.Nombre.ToUpper() == txtNombrePunto.Text.ToUpper());
                if (ubiMaestra == null)
                {
                    ubiMaestra = new Ubicacion { Nombre = txtNombrePunto.Text.ToUpper(), Latitud = _selectedLat, Longitud = _selectedLng, RadioPermitido = 200 };
                    DataService.AgregarUbicacion(ubiMaestra);
                }

                if (_idEmpleadoPreseleccionado > 0)
                {
                    TimeSpan hEntrada = DateTime.Parse(txtHoraInicio.Text).TimeOfDay;
                    TimeSpan hSalida = DateTime.Parse(txtHoraFin.Text).TimeOfDay;
                    var turno = new Turno { Nombre = "PERSONALIZADO", HoraInicio = hEntrada, HoraFin = hSalida };
                    DataService.AgregarTurno(turno);

                    DataService.CrearAsignacion(new Asignacion
                    {
                        IdEmpleado = _idEmpleadoPreseleccionado,
                        IdUbicacion = ubiMaestra.Id,
                        IdTurno = turno.Id,
                        Fecha = dpFecha.SelectedDate ?? _fechaAsignacion,
                        Estatus = "PROGRAMADO"
                    });
                }
                DataService.GuardarTodo();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void BtnEliminarVarios_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = lstUbicaciones.SelectedItems.Cast<Ubicacion>().ToList();
            if (seleccionados.Count > 0)
            {
                // Eliminación silenciosa (sin MessageBox ruidoso)
                foreach (var u in seleccionados) DataService.Ubicaciones.Remove(u);
                DataService.GuardarTodo();
                RefrescarListaUbicaciones();
            }
        }

        private void BtnCancelarSeleccion_Click(object sender, RoutedEventArgs e)
        {
            if (lstUbicaciones != null)
            {
                lstUbicaciones.UnselectAll();
            }
        }
    }
}