using MySql.Data.MySqlClient;
using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Secorvi
{
    public static class DataService
    {
        private static string connectionString = "Server=localhost;Database=secorvi_db;Uid=root;Pwd=2037888;SslMode=Disabled;AllowPublicKeyRetrieval=true;";

        public static List<Empleado> Empleados { get; set; } = new List<Empleado>();
        public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public static List<Turno> Turnos { get; set; } = new List<Turno>();
        public static List<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();

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
            // t.nombre en minúsculas
            if (!Turnos.Any(t => t.nombre.Equals(nombreEstado, StringComparison.OrdinalIgnoreCase)))
            {
                Turno nuevo = new Turno
                {
                    nombre = nombreEstado, // minúscula
                    hora_inicio = new TimeSpan(0, 0, 0), // minúscula
                    hora_fin = new TimeSpan(23, 59, 59) // minúscula
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
                    // Traemos todos para tener la lista completa en el Panel
                    var cmd = new MySqlCommand("SELECT * FROM empleados WHERE estatus = 'Activo'", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Empleados.Add(new Empleado
                            {
                                id_empleado = Convert.ToInt32(r["id_empleado"]),
                                nombre_completo = r["nombre_completo"].ToString(),
                                telefono = r["telefono"].ToString(),
                                id_rol = Convert.ToInt32(r["id_rol"]),
                                estatus = r["estatus"].ToString(),
                                usuario = r["usuario"].ToString(),     // RE-VINCULADO
                                contrasena = r["contrasena"].ToString(),
                                matricula = r["matricula"].ToString()   // RE-VINCULADO
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error: " + ex.Message); }
            }
        }

        public static void AgregarEmpleado(Empleado emp)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "INSERT INTO empleados (nombre_completo, telefono, id_rol, estatus, usuario, contrasena, matricula) " +
                               "VALUES (@nom, @tel, @rol, 'Activo', @usu, @con, @mat)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", emp.nombre_completo);
                cmd.Parameters.AddWithValue("@tel", emp.telefono);
                cmd.Parameters.AddWithValue("@rol", emp.id_rol);
                cmd.Parameters.AddWithValue("@usu", emp.usuario);    // Enviamos el usuario generado
                cmd.Parameters.AddWithValue("@con", emp.contrasena);
                cmd.Parameters.AddWithValue("@mat", emp.matricula);  // Enviamos la matrícula generada

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }

        public static void EliminarEmpleado(int id)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // Cambiamos estatus a 'Inactivo' para no borrar físicamente (Safe Delete)
                string query = "UPDATE empleados SET estatus = 'Inactivo' WHERE id_empleado = @id";
                var cmd = new MySqlCommand(query, conn);
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
                                id_lugar = Convert.ToInt32(r["id_lugar"]),
                                nombre_lugar = r["nombre_lugar"]?.ToString(),
                                latitud = Convert.ToDecimal(r["latitud"]),
                                longitud = Convert.ToDecimal(r["longitud"]),
                                radio_permitido = Convert.ToInt32(r["radio_permitido"])
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
                string query = "INSERT INTO ubicaciones (nombre_lugar, latitud, longitud, radio_permitido) VALUES (@nom, @lat, @lng, @rad)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                cmd.Parameters.AddWithValue("@lat", u.latitud);
                cmd.Parameters.AddWithValue("@lng", u.longitud);
                cmd.Parameters.AddWithValue("@rad", u.radio_permitido);
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
                                id_turno = Convert.ToInt32(r["id_turno"]),
                                id_lugar = r["id_lugar"] != DBNull.Value ? (int?)Convert.ToInt32(r["id_lugar"]) : null,
                                id_empleado = r["id_empleado"] != DBNull.Value ? (int?)Convert.ToInt32(r["id_empleado"]) : null,
                                nombre = r["nombre"].ToString(),
                                hora_inicio = (TimeSpan)r["hora_inicio"],
                                hora_fin = (TimeSpan)r["hora_fin"]
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
                string query = "INSERT INTO turnos (id_lugar, id_empleado, nombre, hora_inicio, hora_fin) VALUES (@lug, @emp, @nom, @ini, @fin)";
                var cmd = new MySqlCommand(query, conn);
                // CORRECCIÓN: t.id_lugar, t.id_empleado, t.nombre, etc. (todo minúscula)
                cmd.Parameters.AddWithValue("@lug", (object)t.id_lugar ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@emp", (object)t.id_empleado ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nom", t.nombre);
                cmd.Parameters.AddWithValue("@ini", t.hora_inicio);
                cmd.Parameters.AddWithValue("@fin", t.hora_fin);
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
                                id_asignaciones = Convert.ToInt32(r["id_asignaciones"]),
                                id_empleado = Convert.ToInt32(r["id_empleado"]),
                                id_ubicacion = r["id_ubicacion"] != DBNull.Value ? Convert.ToInt32(r["id_ubicacion"]) : 0,
                                id_turno = r["id_turno"] != DBNull.Value ? Convert.ToInt32(r["id_turno"]) : 0, // Crucial para el calendario
                                fecha = Convert.ToDateTime(r["fecha"]),
                                estatus = r["estatus"]?.ToString() ?? "PROGRAMADO"
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error: " + ex.Message); }
            }
        }

        public static void CrearAsignacion(Asignacion a)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // Asegúrate de que 'asignaciones' esté escrito igual que en tu DB
                string query = "INSERT INTO asignaciones (id_empleado, id_ubicacion, id_turno, fecha, estatus) " +
                               "VALUES (@emp, @ubi, @tur, @fec, @est)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emp", a.id_empleado);
                cmd.Parameters.AddWithValue("@ubi", a.id_ubicacion == 0 ? (object)DBNull.Value : a.id_ubicacion);
                cmd.Parameters.AddWithValue("@tur", a.id_turno); // Ahora sí enviamos el turno
                cmd.Parameters.AddWithValue("@fec", a.fecha);
                cmd.Parameters.AddWithValue("@est", a.estatus);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarAsignaciones();
        }

        public static void EliminarUbicaciones(List<Ubicacion> lista)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (var ubi in lista)
                {
                    string query = "DELETE FROM ubicaciones WHERE id_lugar = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", ubi.id_lugar);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            CargarUbicaciones();
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

        public static int ObtenerProximoIdEmpleado()
        {
            int proximoId = 1;
            using (var conn = new MySqlConnection(connectionString))
            {
                // Esta consulta le pregunta a MySQL cuál es el siguiente valor del auto_increment
                string query = "SELECT AUTO_INCREMENT FROM information_schema.TABLES " +
                               "WHERE TABLE_SCHEMA = 'secorvi_db' AND TABLE_NAME = 'empleados'";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                try
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        proximoId = Convert.ToInt32(result);
                }
                catch { /* Si falla, retornamos 1 por defecto */ }
            }
            return proximoId;
        }

        public static void CambiarPermisos(int idEmpleado, int nuevoRol)
        {
            using (var conexion = new MySqlConnection(connectionString))
            {
                try
                {
                    conexion.Open();
                    // Actualizamos directamente con el número de rol que recibimos
                    string sql = "UPDATE empleados SET id_rol = @rol WHERE id_empleado = @id";
                    using (var cmd = new MySqlCommand(sql, conexion))
                    {
                        cmd.Parameters.AddWithValue("@rol", nuevoRol);
                        cmd.Parameters.AddWithValue("@id", idEmpleado);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error en CambiarPermisos: " + ex.Message);
                }
            }
            CargarEmpleados(); // Refrescamos la lista global
        }
        public static void ActualizarUbicacion(Ubicacion u)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    // Actualizamos latitud y longitud basándonos en el id_lugar
                    string query = "UPDATE ubicaciones SET latitud = @lat, longitud = @lng, radio_permitido = @rad " +
                                   "WHERE id_lugar = @id";

                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@lat", u.latitud);
                    cmd.Parameters.AddWithValue("@lng", u.longitud);
                    cmd.Parameters.AddWithValue("@rad", u.radio_permitido);
                    cmd.Parameters.AddWithValue("@id", u.id_lugar);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al actualizar ubicación: " + ex.Message);
                }
            }
            // Opcional: Recargar la lista local para que los cambios se reflejen en la UI
            CargarUbicaciones();
        }
        public static int PurgarRegistrosAntiguos(int mesesAntiguedad)
        {
            int filasBorradas = 0;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string sqlAsistencias = "DELETE FROM asistencias WHERE fecha_inicio < DATE_SUB(NOW(), INTERVAL @meses MONTH)";
                        using (var cmd = new MySqlCommand(sqlAsistencias, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@meses", mesesAntiguedad);
                            filasBorradas += cmd.ExecuteNonQuery();
                        }

                        string sqlAsignaciones = "DELETE FROM asignaciones WHERE fecha < DATE_SUB(NOW(), INTERVAL @meses MONTH)";
                        using (var cmd = new MySqlCommand(sqlAsignaciones, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@meses", mesesAntiguedad);
                            filasBorradas += cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return filasBorradas;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        System.Diagnostics.Debug.WriteLine("Error en Purga: " + ex.Message);
                        return -1;
                    }
                }
            }
        }
    }
}