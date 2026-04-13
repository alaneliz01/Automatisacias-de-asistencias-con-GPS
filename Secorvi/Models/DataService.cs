using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
// en este codigo se maneja las reglas para usar cada cosa
namespace Secorvi.Models
{
    public static class DataService
    {
        private static readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string empPath = Path.Combine(baseDir, "empleados.json");
        private static readonly string ubiPath = Path.Combine(baseDir, "ubicaciones.json");
        private static readonly string asigPath = Path.Combine(baseDir, "asignaciones.json");
        private static readonly string configPath = Path.Combine(baseDir, "config.json");

        public static List<Empleado> Empleados { get; set; } = new List<Empleado>();
        public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public static List<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
        public static decimal TarifaHoraGlobal { get; set; } = 35.5m;

        static DataService() { CargarTodo(); }

        public static void CargarTodo()
        {
            try
            {
                // Carga de listas principales
                if (File.Exists(empPath))
                    Empleados = JsonSerializer.Deserialize<List<Empleado>>(File.ReadAllText(empPath)) ?? new List<Empleado>();

                if (File.Exists(ubiPath))
                    Ubicaciones = JsonSerializer.Deserialize<List<Ubicacion>>(File.ReadAllText(ubiPath)) ?? new List<Ubicacion>();

                if (File.Exists(asigPath))
                    Asignaciones = JsonSerializer.Deserialize<List<Asignacion>>(File.ReadAllText(asigPath)) ?? new List<Asignacion>();

                // Carga de configuración (Tarifa)
                if (File.Exists(configPath))
                {
                    var config = JsonSerializer.Deserialize<Dictionary<string, decimal>>(File.ReadAllText(configPath));
                    if (config != null && config.ContainsKey("Tarifa")) TarifaHoraGlobal = config["Tarifa"];
                }

                if (Empleados.Count == 0) CargarSemilla();
            }
            catch (Exception ex) { MessageBox.Show("Error al cargar datos: " + ex.Message); }
        }

        public static void GuardarTodo()
        {
            try
            {
                var opt = new JsonSerializerOptions { WriteIndented = true };

                // Guardamos todas las entidades para no perder integridad
                File.WriteAllText(empPath, JsonSerializer.Serialize(Empleados, opt));
                File.WriteAllText(ubiPath, JsonSerializer.Serialize(Ubicaciones, opt));
                File.WriteAllText(asigPath, JsonSerializer.Serialize(Asignaciones, opt));

                // Guardar la configuración de la tarifa
                var config = new Dictionary<string, decimal> { { "Tarifa", TarifaHoraGlobal } };
                File.WriteAllText(configPath, JsonSerializer.Serialize(config, opt));
            }
            catch (Exception ex) { MessageBox.Show("Error al guardar: " + ex.Message); }
        }

        public static decimal CalcularSueldoEmpleado(Empleado emp)
        {
            decimal totalHorasSemana = 0;
            string[] diasValores = { emp.Lunes, emp.Martes, emp.Miercoles, emp.Jueves, emp.Viernes, emp.Sabado, emp.Domingo };

            foreach (string valor in diasValores)
            {
                if (string.IsNullOrEmpty(valor)) continue;

                if (decimal.TryParse(valor, out decimal horas))
                {
                    totalHorasSemana += horas;
                }
                else if (valor == "D" || valor == "A")
                {
                    totalHorasSemana += 12;
                }
            }
            return totalHorasSemana * TarifaHoraGlobal;
        }

        private static void CargarSemilla()
        {
            Empleados.Add(new Empleado { Id = 1, Matricula = "SEC-2001", Nombre = "AGENTE DE PRUEBA", Telefono = "8110000000", TotalSueldo = 0 });
            GuardarTodo();
        }

        public static List<Empleado> CargarEmpleados() => Empleados;

        public static void GuardarNuevoEmpleado(Empleado nuevo)
        {
            int proximoId = Empleados.Count > 0 ? Empleados.Max(e => e.Id) + 1 : 1;
            nuevo.Id = proximoId;
            Empleados.Add(nuevo);
            GuardarTodo();
        }

        public static void EliminarEmpleado(string matricula)
        {
            var emp = Empleados.FirstOrDefault(x => x.Matricula == matricula);
            if (emp != null) { Empleados.Remove(emp); GuardarTodo(); }
        }

        public static void AsignarDiaLibre(string matricula, string diaSemana)
        {
            var emp = Empleados.FirstOrDefault(x => x.Matricula == matricula);
            if (emp != null)
            {
                string dia = diaSemana.ToLower().Replace("é", "e").Replace("á", "a");
                switch (dia)
                {
                    case "lunes": emp.Lunes = "LIBRE"; break;
                    case "martes": emp.Martes = "LIBRE"; break;
                    case "miercoles": emp.Miercoles = "LIBRE"; break;
                    case "jueves": emp.Jueves = "LIBRE"; break;
                    case "viernes": emp.Viernes = "LIBRE"; break;
                    case "sabado": emp.Sabado = "LIBRE"; break;
                    case "domingo": emp.Domingo = "LIBRE"; break;
                }
                GuardarTodo();
            }
        }
    }
}