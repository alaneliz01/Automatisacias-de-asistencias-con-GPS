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
        private int _idUbicacionSeleccionada = 0; 
        private List<DateTime> _fechasDestino;
       

        public Mapa(int idEmpleado, List<DateTime> fechas)
        {
            InitializeComponent();
            _idEmpleado = idEmpleado;
            _fechasDestino = fechas;

            // 1. Forzamos la fecha de hoy en el selector visual si no hay fechas previas
            if (_fechasDestino == null || _fechasDestino.Count == 0)
            {
                _fechasDestino = new List<DateTime> { DateTime.Today };
            }
            ConfigurarDropdownsHoras();
            RefrescarListaUbicaciones();
            _ = InitMap();
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
                var resultado = MessageBox.Show($"¿Estás seguro de que deseas ocultar la ubicación '{u.nombre_lugar}'?\n\n(Podrás restaurarla después si intentas guardarla de nuevo con el mismo nombre).",
                                                "Confirmar Eliminación",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Tu DataService ya hace el UPDATE estatus = 'Inactivo' aquí:
                        DataService.EliminarUbicacion(u.id_ubicacion);

                        MessageBox.Show("Ubicación eliminada (oculta) correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

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
            lstUbicaciones.ItemsSource = DataService.Ubicaciones
                .Where(u => u.estatus != "Inactivo")
                .OrderBy(u => u.nombre_lugar).ToList();
        }

        // --- APARTADO DE MAPA ---
        private async Task InitMap()
        {
            string cache = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Secorvi", "EBWebView");
            var env = await CoreWebView2Environment.CreateAsync(null, cache);
            await mapaWebView.EnsureCoreWebView2Async(env);

            string html = @"<!DOCTYPE html>
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

        // 1. Agregamos el parámetro isFromList
        function updatePoint(lat, lng, name, move, isFromList = false){ 
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
            
            // 2. Enviamos la bandera de vuelta a C#
            window.chrome.webview.postMessage({lat:lat, lng:lng, name: name || '', isFromList: isFromList}); 
        }

        const geocoder = L.Control.geocoder({
            defaultMarkGeocode: false,
            placeholder: 'Buscar dirección...',
            position: 'topright'
        })
        .on('markgeocode', function(e) {
            var center = e.geocode.center;
            // 3. Geocoder es un punto nuevo (isFromList = false)
            updatePoint(center.lat, center.lng, e.geocode.name, true, false);
        })
        .addTo(map);

        // 4. Click manual es un punto nuevo (isFromList = false)
        map.on('click', (e) => updatePoint(e.latlng.lat, e.latlng.lng, '', false, false)); 
        
        // 5. Carga desde la lista de C# es un punto existente (isFromList = true)
        window.updatePos = (lat, lng) => updatePoint(lat, lng, '', true, true);
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
                _idUbicacionSeleccionada = u.id_ubicacion;
                _selectedLat = (double)u.latitud;
                _selectedLng = (double)u.longitud;

                txtCoords.Text = string.Format(CultureInfo.InvariantCulture, "{0:F6}, {1:F6}", _selectedLat, _selectedLng);
                mapaWebView.ExecuteScriptAsync($"window.updatePos({_selectedLat.ToString(CultureInfo.InvariantCulture)}, {_selectedLng.ToString(CultureInfo.InvariantCulture)})");
                if (u.hora_inicio_default.HasValue)
                    SetPickersFromTimeSpan(u.hora_inicio_default.Value, cbHoraInicio, cbAmPmInicio);

                if (u.hora_fin_default.HasValue)
                    SetPickersFromTimeSpan(u.hora_fin_default.Value, cbHoraFin, cbAmPmFin);
            }
        }
        // --- LÓGICA DE ASIGNACIÓN ---

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validación de seguridad: Asegurarse de tener una ubicación
                if (_idUbicacionSeleccionada == 0 && (_selectedLat == 0 || _selectedLng == 0))
                {
                    MessageBox.Show("SISTEMA: Por favor, selecciona una ubicación de la lista o marca un punto en el mapa para continuar.", "AVISO");
                    return;
                }

                // Tomamos el nombre que esté escrito en el cuadro de texto
                string nombreAsignacion = txtNombrePunto.Text.Trim();
                if (string.IsNullOrEmpty(nombreAsignacion)) nombreAsignacion = "PUNTO DE VIGILANCIA";

                // Obtener las horas seleccionadas en los relojes
                TimeSpan inicio = GetTimeSpanFromPickers(cbHoraInicio, cbAmPmInicio);
                TimeSpan fin = GetTimeSpanFromPickers(cbHoraFin, cbAmPmFin);

                // 2. Gestión de Ubicación (LÓGICA DE RESTAURACIÓN AÑADIDA)
                if (_idUbicacionSeleccionada == 0)
                {
                    // Buscamos si ya existe (activa o inactiva)
                    var ubicacionExistente = DataService.Ubicaciones
                        .FirstOrDefault(u => u.nombre_lugar != null &&
                                             u.nombre_lugar.Equals(nombreAsignacion, StringComparison.OrdinalIgnoreCase));

                    if (ubicacionExistente != null)
                    {
                        if (ubicacionExistente.estatus == "Inactivo")
                        {
                            var respuesta = MessageBox.Show($"La zona '{nombreAsignacion}' fue eliminada anteriormente del sistema.\n\n¿Deseas restaurarla y actualizarla con las nuevas coordenadas y horarios?",
                                                            "UBICACIÓN ENCONTRADA", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (respuesta == MessageBoxResult.Yes)
                            {
                                // Restauramos la ubicación y le actualizamos los nuevos datos del mapa
                                ubicacionExistente.estatus = "Activo";
                                ubicacionExistente.latitud = (decimal)_selectedLat;
                                ubicacionExistente.longitud = (decimal)_selectedLng;
                                ubicacionExistente.hora_inicio_default = inicio;
                                ubicacionExistente.hora_fin_default = fin;

                                DataService.ActualizarUbicacion(ubicacionExistente);
                                _idUbicacionSeleccionada = ubicacionExistente.id_ubicacion;
                                RefrescarListaUbicaciones();
                            }
                            else
                            {
                                return; // Cortamos el proceso si no quiere restaurarla
                            }
                        }
                        else
                        {
                            // Si ya existe y está ACTIVA, NO creamos una nueva. Solo reutilizamos su ID.
                            _idUbicacionSeleccionada = ubicacionExistente.id_ubicacion;
                        }
                    }
                    else
                    {
                        // Si NO existe en absoluto, procedemos a crearla
                        var nuevaUbi = new Ubicacion
                        {
                            nombre_lugar = nombreAsignacion,
                            latitud = (decimal)_selectedLat,
                            longitud = (decimal)_selectedLng,
                            hora_inicio_default = inicio,
                            hora_fin_default = fin,
                            estatus = "Activo" // Nos aseguramos de mandarla como Activa
                        };

                        _idUbicacionSeleccionada = DataService.CrearUbicacionRetornandoId(nuevaUbi);
                        RefrescarListaUbicaciones();
                    }
                }

                // 3. Procesar las fechas
                bool tieneAsignacionHoy = false;

                if (_fechasDestino == null || _fechasDestino.Count == 0)
                {
                    _fechasDestino = new List<DateTime> { DateTime.Today };
                }

                foreach (var fecha in _fechasDestino)
                {
                    bool esHoy = (fecha.Date == DateTime.Today);
                    if (esHoy) tieneAsignacionHoy = true;

                    
                    DataService.EliminarAsignacionPorFecha(_idEmpleado, fecha);

                    var nuevaAsig = new Asignacion
                    {
                        id_empleado = _idEmpleado,
                        id_ubicacion = _idUbicacionSeleccionada,
                        fecha = fecha,
                        hora_inicio = inicio,
                        hora_fin = fin,
                        descripcion_del_turno = nombreAsignacion,
                        estatus = esHoy ? "ACTIVO" : "PROGRAMADO"
                    };

                    DataService.CrearAsignacion(nuevaAsig);
                }

                // 4. Mensajes dinámicos e inteligentes
                string tituloPanel = tieneAsignacionHoy ? "OPERACIÓN ACTIVADA" : "CALENDARIO ACTUALIZADO";

                string mensajeDetalle;
                if (tieneAsignacionHoy && _fechasDestino.Count > 1)
                {
                    mensajeDetalle = $"Se ha ACTIVADO el turno de hoy y se programaron {_fechasDestino.Count - 1} días adicionales correctamente.";
                }
                else if (tieneAsignacionHoy)
                {
                    mensajeDetalle = "El turno para el día de HOY ha sido registrado y activado en el sistema.";
                }
                else
                {
                    mensajeDetalle = $"Se han programado {_fechasDestino.Count} días de servicio para fechas posteriores.";
                }

                MessageBox.Show(mensajeDetalle, tituloPanel, MessageBoxButton.OK, MessageBoxImage.Information);

                // Volver a la pantalla anterior (Calendario/Panel Principal)
                this.NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR CRÍTICO AL GUARDAR: " + ex.Message, "SISTEMA FALLIDO", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBusqueda.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(filtro))
            {
                RefrescarListaUbicaciones();
            }
            else
            {
                var ubicacionesFiltradas = DataService.Ubicaciones
                    .Where(u => u.estatus != "Inactivo" && u.nombre_lugar != null && u.nombre_lugar.ToLower().Contains(filtro))
                    .OrderBy(u => u.nombre_lugar)
                    .ToList();

                lstUbicaciones.ItemsSource = ubicacionesFiltradas;
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

            if (h >= 12)
            {
                amPm = "PM";
                if (h > 12) h -= 12;
            }
            if (h == 0) h = 12;

            string horaBuscada = $"{h:D2}:{ts.Minutes:D2}";

            foreach (var item in cbHora.Items)
            {
                if (item.ToString() == horaBuscada)
                {
                    cbHora.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in cbAmPm.Items)
            {
                if (item.Content.ToString() == amPm)
                {
                    cbAmPm.SelectedItem = item;
                    break;
                }
            }
        }
        private void BtnTurno8_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(8);
        private void BtnTurno12_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(12);
        private void BtnTurno24_Click(object sender, RoutedEventArgs e) => AplicarPreajuste(24);
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