using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Secorvi.Models
{
    public static class DataService
    {
        // Rutas de archivos JSON (Simulación de tablas de BD)
        private static readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string empPath = Path.Combine(baseDir, "empleados.json");
        private static readonly string ubiPath = Path.Combine(baseDir, "ubicaciones.json");
        private static readonly string asigPath = Path.Combine(baseDir, "asignaciones.json");
        private static readonly string turnPath = Path.Combine(baseDir, "turnos.json");
        private static readonly string asistPath = Path.Combine(baseDir, "asistencias.json");

        // Listas en memoria (Cache de datos)
        public static List<Empleado> Empleados { get; set; } = new List<Empleado>();
        public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public static List<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
        public static List<Turno> Turnos { get; set; } = new List<Turno>();
        public static List<Asistencia> HistorialAsistencias { get; set; } = new List<Asistencia>();

        static DataService()
        {
            CargarTodo();
        }

        #region Persistencia de Datos
        public static void CargarTodo()
        {
            try
            {
                if (File.Exists(empPath))
                    Empleados = JsonSerializer.Deserialize<List<Empleado>>(File.ReadAllText(empPath)) ?? new List<Empleado>();

                if (File.Exists(ubiPath))
                    Ubicaciones = JsonSerializer.Deserialize<List<Ubicacion>>(File.ReadAllText(ubiPath)) ?? new List<Ubicacion>();

                if (File.Exists(asigPath))
                    Asignaciones = JsonSerializer.Deserialize<List<Asignacion>>(File.ReadAllText(asigPath)) ?? new List<Asignacion>();

                if (File.Exists(turnPath))
                    Turnos = JsonSerializer.Deserialize<List<Turno>>(File.ReadAllText(turnPath)) ?? new List<Turno>();

                if (File.Exists(asistPath))
                    HistorialAsistencias = JsonSerializer.Deserialize<List<Asistencia>>(File.ReadAllText(asistPath)) ?? new List<Asistencia>();

                if (Empleados.Count == 0) CargarSemilla();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico al cargar base de datos local: " + ex.Message);
            }
        }

        public static void GuardarTodo()
        {
            try
            {
                var opt = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(empPath, JsonSerializer.Serialize(Empleados, opt));
                File.WriteAllText(ubiPath, JsonSerializer.Serialize(Ubicaciones, opt));
                File.WriteAllText(asigPath, JsonSerializer.Serialize(Asignaciones, opt));
                File.WriteAllText(turnPath, JsonSerializer.Serialize(Turnos, opt));
                File.WriteAllText(asistPath, JsonSerializer.Serialize(HistorialAsistencias, opt));
            }
            catch (Exception ex) { MessageBox.Show("Error al guardar persistencia: " + ex.Message); }
        }
        #endregion

        #region Lógica de Validación Geofencing (Simulación Bot WhatsApp)
        public static bool ValidarEntradaEmpleado(string matricula, double latEnviada, double lonEnviada)
        {
            var emp = Empleados.FirstOrDefault(e => e.Matricula == matricula);
            if (emp == null) return false;

            // Busca asignación activa para el día de hoy
            var hoy = DateTime.Today;
            var asignacion = Asignaciones.FirstOrDefault(a => a.IdEmpleado == emp.Id && a.Fecha.Date == hoy);

            if (asignacion == null) return false;

            // Obtiene los datos geográficos del lugar asignado
            var ubi = Ubicaciones.FirstOrDefault(u => u.Id == asignacion.IdUbicacion);
            if (ubi == null) return false;

            // Cálculo de distancia real
            double distancia = CalcularDistanciaMetros(latEnviada, lonEnviada, ubi.Latitud, ubi.Longitud);
            bool cumpleDistancia = distancia <= ubi.RadioPermitido;

            // Registro del evento en la tabla Asistencia
            var registro = new Asistencia
            {
                Id = HistorialAsistencias.Count > 0 ? HistorialAsistencias.Max(a => a.Id) + 1 : 1,
                IdEmpleado = emp.Id,
                FechaHora = DateTime.Now,
                LatitudRecibida = latEnviada,
                LongitudRecibida = lonEnviada,
                DentroDeRango = cumpleDistancia,
                Incidencias = cumpleDistancia ? "ASISTENCIA CORRECTA" : $"FUERA DE RANGO ({Math.Round(distancia)}m)"
            };

            HistorialAsistencias.Add(registro);
            GuardarTodo();

            return cumpleDistancia;
        }

        private static double CalcularDistanciaMetros(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371000; // Radio de la tierra en metros
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        #endregion

        #region Gestión de Entidades
        public static void GuardarNuevoEmpleado(Empleado nuevo)
        {
            nuevo.Id = Empleados.Count > 0 ? Empleados.Max(e => e.Id) + 1 : 1;
            Empleados.Add(nuevo);
            GuardarTodo();
        }

        public static void EliminarEmpleado(int idEmpleado)
        {
            var emp = Empleados.FirstOrDefault(x => x.Id == idEmpleado);
            if (emp != null && !emp.EsSuperAdmin)
            {
                Empleados.Remove(emp);
                // Limpieza de cascada manual para el simulador JSON
                Asignaciones.RemoveAll(a => a.IdEmpleado == idEmpleado);
                GuardarTodo();
            }
        }

        public static void AgregarUbicacion(Ubicacion nueva)
        {
            nueva.Id = Ubicaciones.Count > 0 ? Ubicaciones.Max(u => u.Id) + 1 : 1;
            Ubicaciones.Add(nueva);
            GuardarTodo();
        }

        public static void AgregarTurno(Turno nuevo)
        {
            nuevo.Id = Turnos.Count > 0 ? Turnos.Max(t => t.Id) + 1 : 1;
            Turnos.Add(nuevo);
            GuardarTodo();
        }

        public static void CrearAsignacion(Asignacion nueva)
        {
            nueva.Id = Asignaciones.Count > 0 ? Asignaciones.Max(a => a.Id) + 1 : 1;
            Asignaciones.Add(nueva);
            GuardarTodo();
        }
        #endregion

        private static void CargarSemilla()
        {
            // Admin por defecto
            if (!Empleados.Any())
            {
                Empleados.Add(new Empleado
                {
                    Id = 1,
                    Matricula = "SEC-001",
                    Nombre = "Rommel",
                    Password = "1234",
                    EsAdmin = true,
                    EsSuperAdmin = true,
                    TurnoTipo = "ADMIN"
                });
            }

            // Agregar una ubicación de prueba si no hay
            if (!Ubicaciones.Any())
            {
                Ubicaciones.Add(new Ubicacion { Id = 1, Nombre = "Oficina Central", Latitud = 25.6866, Longitud = -100.3161, RadioPermitido = 100 });
            }

            GuardarTodo();
        }
    }
}