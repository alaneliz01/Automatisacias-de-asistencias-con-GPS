using MySql.Data.MySqlClient;
using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Globalization;

namespace Secorvi
{
    public static class DataService
    {
        private static string connectionString = "Server=localhost;Database=secorvi_db;Uid=root;Pwd=2037888;AllowPublicKeyRetrieval=true;";

        public static List<Empleado> Empleados { get; set; } = new List<Empleado>();
        public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public static List<Turno> Turnos { get; set; } = new List<Turno>();
        public static List<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();

        public static void GuardarTodo() => ActualizarTodo();

        public static void ActualizarTodo()
        {
            try
            {
                CargarEmpleados();
                CargarUbicaciones();
                CargarTurnos();
                VerificarEstadosEspeciales();
                CargarAsignaciones();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Sync Error: " + ex.Message);
            }
        }

        private static void VerificarEstadosEspeciales()
        {
            CrearTurnoSistemaSiNoExiste("VACACIONES");
            CrearTurnoSistemaSiNoExiste("DÍA LIBRE");
        }

        private static void CrearTurnoSistemaSiNoExiste(string nombreEstado)
        {
            if (!Turnos.Any(t => t.Nombre.Equals(nombreEstado, StringComparison.OrdinalIgnoreCase)))
            {
                Turno nuevo = new Turno
                {
                    Nombre = nombreEstado,
                    HoraInicio = new TimeSpan(0, 0, 0),
                    HoraFin = new TimeSpan(23, 59, 59)
                };
                AgregarTurno(nuevo);
            }
        }

        // --- GESTIÓN DE EMPLEADOS ---
        public static void CargarEmpleados()
        {
            Empleados.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM empleados WHERE activo = 1", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Empleados.Add(new Empleado
                            {
                                Id = r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0,
                                Nombre = r["nombre"]?.ToString() ?? "",
                                Apellido = r["apellido"]?.ToString() ?? "",
                                Matricula = r["matricula"]?.ToString() ?? "",
                                Telefono = r["telefono"]?.ToString() ?? "",
                                Usuario = r["usuario"]?.ToString()?.Trim() ?? "",
                                Contrasena = r["contrasena"]?.ToString()?.Trim() ?? "",
                                EsAdmin = r["es_admin"] != DBNull.Value && Convert.ToBoolean(r["es_admin"]),
                                Activo = r["activo"] != DBNull.Value && Convert.ToBoolean(r["activo"])
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }
        }

        public static void AgregarEmpleado(Empleado emp)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "INSERT INTO empleados (nombre, apellido, matricula, telefono, usuario, contrasena, es_admin, activo) VALUES (@nom, @ape, @mat, @tel, @usu, @con, @adm, 1)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", emp.Nombre);
                cmd.Parameters.AddWithValue("@ape", emp.Apellido);
                cmd.Parameters.AddWithValue("@mat", emp.Matricula);
                cmd.Parameters.AddWithValue("@tel", emp.Telefono);
                cmd.Parameters.AddWithValue("@usu", emp.Usuario);
                cmd.Parameters.AddWithValue("@con", emp.Contrasena);
                cmd.Parameters.AddWithValue("@adm", emp.EsAdmin);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }

        public static void EliminarEmpleado(int id)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "UPDATE empleados SET activo = 0 WHERE id = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }

        public static void CambiarPermisos(int id, bool esAdmin)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "UPDATE empleados SET es_admin = @adm WHERE id = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@adm", esAdmin);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }

        // --- GESTIÓN DE UBICACIONES ---
        public static void CargarUbicaciones()
        {
            Ubicaciones.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM ubicaciones", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Ubicaciones.Add(new Ubicacion
                            {
                                Id = Convert.ToInt32(r["id"]),
                                Nombre = r["nombre"]?.ToString(),
                                Latitud = Convert.ToDouble(r["latitud"]),
                                Longitud = Convert.ToDouble(r["longitud"]),
                                RadioPermitido = Convert.ToDouble(r["radio_permitido"]),
                                Activo = Convert.ToBoolean(r["activo"])
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }
        }

        public static void AgregarUbicacion(Ubicacion u)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "INSERT INTO ubicaciones (nombre, latitud, longitud, radio_permitido, activo) VALUES (@nom, @lat, @lng, @rad, 1)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", u.Nombre);
                cmd.Parameters.AddWithValue("@lat", u.Latitud);
                cmd.Parameters.AddWithValue("@lng", u.Longitud);
                cmd.Parameters.AddWithValue("@rad", u.RadioPermitido);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarUbicaciones();
        }

        // --- GESTIÓN DE TURNOS ---
        public static void CargarTurnos()
        {
            Turnos.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM turnos", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Turnos.Add(new Turno
                            {
                                Id = Convert.ToInt32(r["id"]),
                                Nombre = r["nombre"].ToString(),
                                HoraInicio = (TimeSpan)r["hora_inicio"],
                                HoraFin = (TimeSpan)r["hora_fin"]
                            });
                        }
                    }
                }
                catch { }
            }
        }

        public static void AgregarTurno(Turno t)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "INSERT INTO turnos (nombre, hora_inicio, hora_fin) VALUES (@nom, @ini, @fin)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", t.Nombre);
                cmd.Parameters.AddWithValue("@ini", t.HoraInicio);
                cmd.Parameters.AddWithValue("@fin", t.HoraFin);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarTurnos();
        }

        // --- GESTIÓN DE ASIGNACIONES ---
        public static void CargarAsignaciones()
        {
            Asignaciones.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM asignaciones", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Asignaciones.Add(new Asignacion
                            {
                                Id = Convert.ToInt32(r["id"]),
                                IdEmpleado = Convert.ToInt32(r["id_empleado"]),
                                IdUbicacion = r["id_ubicacion"] != DBNull.Value ? Convert.ToInt32(r["id_ubicacion"]) : 0,
                                IdTurno = Convert.ToInt32(r["id_turno"]),
                                Fecha = Convert.ToDateTime(r["fecha"]),
                                Estatus = r["estatus"]?.ToString() ?? "PENDIENTE"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al cargar asignaciones: " + ex.Message);
                }
            }
        }

        public static void CrearAsignacion(Asignacion a)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "INSERT INTO asignaciones (id_empleado, id_ubicacion, id_turno, fecha, estatus) VALUES (@emp, @ubi, @tur, @fec, @est)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emp", a.IdEmpleado);
                cmd.Parameters.AddWithValue("@ubi", a.IdUbicacion == 0 ? (object)DBNull.Value : a.IdUbicacion);
                cmd.Parameters.AddWithValue("@tur", a.IdTurno);
                cmd.Parameters.AddWithValue("@fec", a.Fecha);
                cmd.Parameters.AddWithValue("@est", a.Estatus);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarAsignaciones();
        }

        public static void EliminarAsignacionPorFecha(int idEmpleado, DateTime fecha)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "DELETE FROM asignaciones WHERE id_empleado = @emp AND DATE(fecha) = DATE(@fec)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emp", idEmpleado);
                cmd.Parameters.AddWithValue("@fec", fecha);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarAsignaciones();
        }
    }
}