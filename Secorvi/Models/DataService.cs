using MySql.Data.MySqlClient;
using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Secorvi
{
    public class AsignacionReporte
    {
        public int id_asignacion { get; set; }
        public string matricula { get; set; } 
        public string nombre_agente { get; set; }
        public string telefono { get; set; } 
        public string nombre_lugar { get; set; }
        public string rango_horario { get; set; }
        public string estatus { get; set; }
        public DateTime fecha { get; set; }
    }
    public static class DataService
    {
        private static string connectionString = "Server=localhost;Database=secorvi_db;Uid=root;Pwd=root;SslMode=Disabled;AllowPublicKeyRetrieval=true;";

        public static List<Empleado> Empleados { get; set; } = new List<Empleado>();
        public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public static List<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();

        public static void ActualizarTodo()
        {
            CargarUbicaciones();
            CargarEmpleados();
            CargarAsignaciones();
            SincronizarEstatusVistaJefe();
        }

        private static void SincronizarEstatusVistaJefe()
        {
            foreach (var emp in Empleados)
            {
                var asig = Asignaciones.FirstOrDefault(a => a.id_empleado == emp.id_empleado && a.fecha.Date == DateTime.Today);
                if (asig != null)
                {
                    emp.estatus_asistencia = asig.estatus;
                    emp.info_turno = $"{asig.descripcion_del_turno}: {asig.hora_inicio:hh\\:mm} - {asig.hora_fin:hh\\:mm}";
                }
                else
                {
                    emp.estatus_asistencia = "SIN PROGRAMAR";
                    emp.info_turno = "N/A";
                }
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
                    // Seleccionamos empleados activos
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
                                usuario = r["usuario"].ToString(),
                                contrasena = r["contrasena"].ToString(),
                                matricula = r["matricula"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error Empleados: " + ex.Message); }
            }
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
                                id_ubicacion = Convert.ToInt32(r["id_ubicacion"]), // Antes decía id_lugar
                                nombre_lugar = r["nombre_lugar"].ToString(),
                                latitud = Convert.ToDecimal(r["latitud"]),
                                longitud = Convert.ToDecimal(r["longitud"]),
                                radio_permitido = Convert.ToInt32(r["radio_permitido"])
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error Ubicaciones: " + ex.Message); }
            }
        }

        // --- GESTIÓN DE ASIGNACIONES (FUSIONADA CON TURNOS) ---
        public static void CargarAsignaciones()
        {
            Asignaciones.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // El query ahora refleja la estructura real de la tabla 'asignaciones'
                    var cmd = new MySqlCommand("SELECT * FROM asignaciones", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Asignaciones.Add(new Asignacion
                            {
                                id_asignacion = Convert.ToInt32(r["id_asignacion"]),
                                id_empleado = Convert.ToInt32(r["id_empleado"]),
                                id_ubicacion = Convert.ToInt32(r["id_ubicacion"]),
                                descripcion_del_turno = r["descripcion_del_turno"].ToString(),
                                fecha = Convert.ToDateTime(r["fecha"]),
                                hora_inicio = (TimeSpan)r["hora_inicio"],
                                hora_fin = (TimeSpan)r["hora_fin"],
                                estatus = r["estatus"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error Asignaciones: " + ex.Message); }
            }
        }


        public static void CrearAsignacion(Asignacion a)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = @"INSERT INTO asignaciones 
                        (id_empleado, id_ubicacion, descripcion_del_turno, fecha, hora_inicio, hora_fin, estatus) 
                        VALUES (@emp, @ubi, @desc, @fec, @ini, @fin, @est)";

                var cmd = new MySqlCommand(query, conn);

                // 1. Manejo de IDs y Fechas (Sin cambios)
                cmd.Parameters.AddWithValue("@emp", a.id_empleado);
                cmd.Parameters.AddWithValue("@ubi", a.id_ubicacion <= 0 ? 1 : a.id_ubicacion);
                cmd.Parameters.AddWithValue("@fec", a.fecha.Date);
                cmd.Parameters.AddWithValue("@ini", a.hora_inicio);
                cmd.Parameters.AddWithValue("@fin", a.hora_fin);

                // 2. Descripción: Forzamos máximo 16 caracteres (límite de tu DB)
                string d = (a.descripcion_del_turno ?? "").Trim();
                if (d.Length > 16) d = d.Substring(0, 16);
                cmd.Parameters.Add("@desc", MySqlDbType.VarChar, 16).Value = d;
                string e = (a.estatus ?? "PROGRAMADO").Trim().ToUpper();
                if (e.Length > 16) e = e.Substring(0, 16);

                cmd.Parameters.Add("@est", MySqlDbType.VarChar).Value = e;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarAsignaciones();
        }

        public static void EliminarAsignacion(int idAsignacion)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "DELETE FROM asignaciones WHERE id_asignacion = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idAsignacion);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarAsignaciones();
        }
        public static List<AsignacionReporte> ObtenerAsignacionesCompletas()
        {
            List<AsignacionReporte> lista = new List<AsignacionReporte>();
            string sql = @"
        SELECT 
            a.id_asignacion, 
            e.matricula, 
            e.nombre_completo AS nombre_agente, 
            e.telefono,
            u.nombre_lugar, 
            CONCAT(TIME_FORMAT(a.hora_inicio, '%H:%i'), ' - ', TIME_FORMAT(a.hora_fin, '%H:%i')) AS rango_horario,
            a.estatus,
            a.fecha
        FROM asignaciones a
        INNER JOIN empleados e ON a.id_empleado = e.id_empleado
        INNER JOIN ubicaciones u ON a.id_ubicacion = u.id_ubicacion
        WHERE a.fecha = CURDATE();";

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                lista.Add(new AsignacionReporte
                                {
                                    id_asignacion = Convert.ToInt32(r["id_asignacion"]),
                                    matricula = r["matricula"].ToString(),
                                    nombre_agente = r["nombre_agente"].ToString(),
                                    telefono = r["telefono"].ToString(),
                                    nombre_lugar = r["nombre_lugar"].ToString(),
                                    rango_horario = r["rango_horario"].ToString(),
                                    estatus = r["estatus"].ToString(),
                                    fecha = Convert.ToDateTime(r["fecha"])
                                });
                            }
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }
            return lista;
        }
        public static int ObtenerProximoIdEmpleado()
        {
            int proximoId = 1;
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = @"SELECT AUTO_INCREMENT 
                                FROM information_schema.TABLES 
                                WHERE TABLE_SCHEMA = 'secorvi_db' 
                                AND TABLE_NAME = 'empleados'";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                try
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        proximoId = Convert.ToInt32(result);
                }
                catch { /* Retornamos 1 por defecto si hay error */ }
            }
            return proximoId;
        }
        public static void AgregarEmpleado(Empleado emp)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // El estatus siempre inicia en 'Activo' por defecto
                string query = @"INSERT INTO empleados 
                                (nombre_completo, telefono, id_rol, estatus, usuario, contrasena, matricula) 
                                VALUES (@nom, @tel, @rol, 'Activo', @usu, @con, @mat)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", emp.nombre_completo);
                cmd.Parameters.AddWithValue("@tel", emp.telefono);
                cmd.Parameters.AddWithValue("@rol", emp.id_rol);
                cmd.Parameters.AddWithValue("@usu", emp.usuario);
                cmd.Parameters.AddWithValue("@con", emp.contrasena);
                cmd.Parameters.AddWithValue("@mat", emp.matricula);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // Refrescamos la lista local para que el Panel de Control vea al nuevo empleado
            CargarEmpleados();
        }

        // --- GESTIÓN DE EMPLEADOS: BAJA LÓGICA ---
        public static void EliminarEmpleado(int id)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // Siguiendo tu lógica de 'Baja Lógica' para no romper integridad referencial
                string query = "UPDATE empleados SET estatus = 'Inactivo' WHERE id_empleado = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }
        // --- GESTIÓN DE UBICACIONES ---
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

        public static void ActualizarUbicacion(Ubicacion u)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // SQL corregido con id_ubicacion
                string query = "UPDATE ubicaciones SET latitud = @lat, longitud = @lng, radio_permitido = @rad WHERE id_ubicacion = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@lat", u.latitud);
                cmd.Parameters.AddWithValue("@lng", u.longitud);
                cmd.Parameters.AddWithValue("@rad", u.radio_permitido);
                cmd.Parameters.AddWithValue("@id", u.id_ubicacion); // Antes u.id_lugar
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarUbicaciones();
        }

        public static void EliminarUbicaciones(List<Ubicacion> lista)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                foreach (var ubi in lista)
                {
                    // CORRECCIÓN: DELETE FROM ubicaciones WHERE id_ubicacion = @id
                    string query = "DELETE FROM ubicaciones WHERE id_ubicacion = @id"; // Antes id_lugar
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", ubi.id_ubicacion);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            CargarUbicaciones();
        }

        // --- GESTIÓN DE EMPLEADOS: ROLES ---
        public static void CambiarPermisos(int idEmpleado, int nuevoRol)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "UPDATE empleados SET id_rol = @rol WHERE id_empleado = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@rol", nuevoRol);
                cmd.Parameters.AddWithValue("@id", idEmpleado);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }
        public static List<AsignacionDetalle> ObtenerAsignacionesDetalladas()
        {
            List<AsignacionDetalle> lista = new List<AsignacionDetalle>();
            string sql = @"
        SELECT 
            a.id_asignacion, 
            e.id_empleado,
            e.nombre_completo AS empleado, 
            u.nombre_lugar AS ubicacion, 
            CONCAT(TIME_FORMAT(a.hora_inicio, '%H:%i'), ' - ', TIME_FORMAT(a.hora_fin, '%H:%i')) AS turno,
            a.estatus,
            a.fecha
        FROM asignaciones a
        INNER JOIN empleados e ON a.id_empleado = e.id_empleado
        INNER JOIN ubicaciones u ON a.id_ubicacion = u.id_ubicacion";

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new AsignacionDetalle
                            {
                                id_asignaciones = Convert.ToInt32(r["id_asignacion"]),
                                id_empleado = Convert.ToInt32(r["id_empleado"]),
                                empleado = r["empleado"].ToString(),
                                ubicacion = r["ubicacion"].ToString(),
                                turno = r["turno"].ToString(),
                                estatus = r["estatus"].ToString(),
                                fecha = Convert.ToDateTime(r["fecha"])
                            });
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }
            return lista;
        }
        // --- GESTIÓN DE ASIGNACIONES: ELIMINACIÓN POR FECHA ---
        public static void EliminarAsignacionPorFecha(int idEmpleado, DateTime fecha)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // Usamos DATE(@fec) para asegurarnos de comparar solo la fecha sin la hora
                string query = "DELETE FROM asignaciones WHERE id_empleado = @emp AND DATE(fecha) = DATE(@fec)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emp", idEmpleado);
                cmd.Parameters.AddWithValue("@fec", fecha);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al eliminar asignación: " + ex.Message);
                }
            }
            CargarAsignaciones(); // Refrescamos la lista global
        }
    }
}