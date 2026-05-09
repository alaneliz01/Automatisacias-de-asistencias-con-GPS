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
        // Tu conexión a Docker (Esto está bien)
        private static string connectionString = "Server=svukid.easypanel.host;Port=33060;Database=secorvi-db;Uid=admin;Pwd=admin;SslMode=Disabled;AllowPublicKeyRetrieval=true;"; public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();

        // 2. Tienes la lista de Asignaciones
        public static List<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();

        public static List<Empleado> Empleados { get; set; } = new List<Empleado>();

        public static void ActualizarTodo()
        {
            CargarUbicaciones();
            CargarEmpleados();
            CargarAsignaciones();
            SincronizarEstatusVistaJefe();
        }

        private static void SincronizarEstatusVistaJefe()
        {
            // 1. Consultar estado y HORAS REALES de n8n
            var asistenciasHoy = new Dictionary<int, (string estado, TimeSpan? entrada, TimeSpan? salida)>();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Traemos el estado y las horas reales registradas en la tabla 'asistencias'
                    string query = @"SELECT id_empleado, estado, hora_inicio, hora_fin 
                             FROM asistencias 
                             WHERE DATE(fecha_inicio) = CURDATE() OR DATE(fecha_fin) = CURDATE()";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string est = r["estado"] != DBNull.Value ? r["estado"].ToString().ToUpper() : null;
                            TimeSpan? ent = r["hora_inicio"] != DBNull.Value ? (TimeSpan)r["hora_inicio"] : (TimeSpan?)null;
                            TimeSpan? sal = r["hora_fin"] != DBNull.Value ? (TimeSpan)r["hora_fin"] : (TimeSpan?)null;

                            asistenciasHoy[Convert.ToInt32(r["id_empleado"])] = (est, ent, sal);
                        }
                    }
                }
                catch { /* Si hay error, simplemente continúa con los estatus programados */ }
            }

            // 2. Sincronizar con la lista de Empleados de la vista
            foreach (var emp in Empleados.ToList())
            {
                var asig = Asignaciones.ToList().FirstOrDefault(a => a.id_empleado == emp.id_empleado && a.fecha.Date == DateTime.Today);

                if (asig != null)
                {
                    // Si n8n tiene un registro de hoy (Asistencia o Salida)
                    if (asistenciasHoy.ContainsKey(emp.id_empleado))
                    {
                        var real = asistenciasHoy[emp.id_empleado];
                        emp.estatus_asistencia = real.estado ?? asig.estatus;

                        // Formateamos para mostrar las horas reales
                        string horaIn = real.entrada.HasValue ? real.entrada.Value.ToString(@"hh\:mm") : asig.hora_inicio.ToString(@"hh\:mm");
                        string horaOut = real.salida.HasValue ? real.salida.Value.ToString(@"hh\:mm") : asig.hora_fin.ToString(@"hh\:mm");

                        if (real.estado == "SALIDA")
                            emp.info_turno = $"FINALIZADO: {horaIn} - {horaOut}";
                        else
                            emp.info_turno = $"EN CURSO: {horaIn} - {horaOut}";
                    }
                    else
                    {
                        // Si aún no checan, mostramos lo programado
                        emp.estatus_asistencia = asig.estatus;

                        if (asig.hora_inicio == TimeSpan.Zero && asig.hora_fin == TimeSpan.Zero && asig.estatus != "DESCANSO")
                        {
                            emp.info_turno = $"{asig.descripcion_del_turno}: 24 HORAS";
                        }
                        else
                        {
                            emp.info_turno = $"{asig.descripcion_del_turno}: {asig.hora_inicio:hh\\:mm} - {asig.hora_fin:hh\\:mm}";
                        }
                    }
                }
                else
                {
                    emp.estatus_asistencia = "SIN PROGRAMAR";
                    emp.info_turno = "N/A";
                }
            }
        }        // --- GESTIÓN DE EMPLEADOS ---
        public static void CargarEmpleados()
        {
            Empleados.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

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
                catch (Exception ex)
                {
                    // Esto nos obligará a ver el error en la pantalla
                    System.Windows.MessageBox.Show("Error conectando a BD (Empleados): " + ex.Message);
                }
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
                                id_ubicacion = Convert.ToInt32(r["id_ubicacion"]),
                                nombre_lugar = r["nombre_lugar"].ToString(),
                                latitud = Convert.ToDecimal(r["latitud"]),
                                longitud = Convert.ToDecimal(r["longitud"]),
                                radio_permitido = Convert.ToInt32(r["radio_permitido"])
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error cargando ubicaciones: " + ex.Message);
                }
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


                cmd.Parameters.AddWithValue("@emp", a.id_empleado);
                cmd.Parameters.AddWithValue("@ubi", a.id_ubicacion <= 0 ? 1 : a.id_ubicacion);
                cmd.Parameters.AddWithValue("@fec", a.fecha.Date);
                cmd.Parameters.AddWithValue("@ini", a.hora_inicio);
                cmd.Parameters.AddWithValue("@fin", a.hora_fin);

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
                conn.Open();
                var transaction = conn.BeginTransaction();
                try
                {
                    // Opcional: Inactivar las asistencias relacionadas en lugar de borrarlas
                    string queryHijos = "UPDATE asistencias SET estado = 'INACTIVO' WHERE id_asignacion = @id";
                    var cmd1 = new MySqlCommand(queryHijos, conn, transaction);
                    cmd1.Parameters.AddWithValue("@id", idAsignacion);
                    cmd1.ExecuteNonQuery();

                    // Inactivar la asignación
                    string queryPadre = "UPDATE asignaciones SET estatus = 'INACTIVO' WHERE id_asignacion = @id";
                    var cmd2 = new MySqlCommand(queryPadre, conn, transaction);
                    cmd2.Parameters.AddWithValue("@id", idAsignacion);
                    cmd2.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
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
                string query = "SELECT IFNULL(MAX(id_empleado), 0) + 1 FROM empleados";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                try
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        proximoId = Convert.ToInt32(result);
                }
                catch { }
            }
            return proximoId;
        }

        public static void AgregarEmpleado(Empleado emp)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string checkQuery = "SELECT id_empleado, estatus FROM empleados WHERE matricula = @mat";
                int? idExistente = null;
                string estatusActual = null;

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@mat", emp.matricula);
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idExistente = Convert.ToInt32(reader["id_empleado"]);
                            estatusActual = reader["estatus"].ToString();
                        }
                    }
                }

                if (idExistente.HasValue)
                {
                    // 2. Si existe, hacemos un UPDATE para reactivarlo y sobreescribir sus nuevos datos
                    string updateQuery = @"UPDATE empleados 
                                   SET nombre_completo = @nom, telefono = @tel, id_rol = @rol, 
                                       estatus = 'Activo', usuario = @usu, contrasena = @con 
                                   WHERE id_empleado = @id";
                    using (var updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@id", idExistente.Value);
                        updateCmd.Parameters.AddWithValue("@nom", emp.nombre_completo);
                        updateCmd.Parameters.AddWithValue("@tel", emp.telefono);
                        updateCmd.Parameters.AddWithValue("@rol", emp.id_rol);
                        updateCmd.Parameters.AddWithValue("@usu", emp.usuario);
                        updateCmd.Parameters.AddWithValue("@con", emp.contrasena);
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // 3. Si no existe, hacemos el INSERT normal
                    string insertQuery = @"INSERT INTO empleados 
                    (id_empleado, nombre_completo, telefono, id_rol, estatus, usuario, contrasena, matricula) 
                    VALUES (@id, @nom, @tel, @rol, 'Activo', @usu, @con, @mat)";

                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@id", ObtenerProximoIdEmpleado()); // Usamos tu método
                        insertCmd.Parameters.AddWithValue("@nom", emp.nombre_completo);
                        insertCmd.Parameters.AddWithValue("@tel", emp.telefono);
                        insertCmd.Parameters.AddWithValue("@rol", emp.id_rol);
                        insertCmd.Parameters.AddWithValue("@usu", emp.usuario);
                        insertCmd.Parameters.AddWithValue("@con", emp.contrasena);
                        insertCmd.Parameters.AddWithValue("@mat", emp.matricula);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            CargarEmpleados();
        }

        public static void EliminarEmpleado(int id)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Inactivamos las asignaciones futuras del empleado (Opcional, pero recomendado)
                string queryAsignaciones = "UPDATE asignaciones SET estatus = 'INACTIVO' WHERE id_empleado = @id AND fecha >= CURDATE()";
                using (var cmdAsig = new MySqlCommand(queryAsignaciones, conn))
                {
                    cmdAsig.Parameters.AddWithValue("@id", id);
                    cmdAsig.ExecuteNonQuery();
                }

                // En lugar de DELETE, actualizamos el estatus
                string queryEmpleado = "UPDATE empleados SET estatus = 'Inactivo' WHERE id_empleado = @id";
                using (var cmdEmp = new MySqlCommand(queryEmpleado, conn))
                {
                    cmdEmp.Parameters.AddWithValue("@id", id);
                    cmdEmp.ExecuteNonQuery();
                }
            }

            CargarEmpleados();
            CargarAsignaciones();
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
                string query = @"UPDATE ubicaciones 
                         SET nombre_lugar = @nom, 
                             latitud = @lat, 
                             longitud = @lng, 
                             radio_permitido = @rad
                         WHERE id_ubicacion = @id";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                cmd.Parameters.AddWithValue("@lat", u.latitud);
                cmd.Parameters.AddWithValue("@lng", u.longitud);
                cmd.Parameters.AddWithValue("@rad", u.radio_permitido);
                cmd.Parameters.AddWithValue("@id", u.id_ubicacion);

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
                foreach (var ubi in lista.ToList())
                {
                    // Cambiamos DELETE por UPDATE
                    string query = "UPDATE ubicaciones SET estatus = 'Inactivo' WHERE id_ubicacion = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", ubi.id_ubicacion);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            CargarUbicaciones();
        }
        public static void ActualizarEmpleado(Empleado emp)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // 1. Armamos la consulta dinámica. 
                // Actualizamos nombre, teléfono, usuario y rol por defecto.
                string query = @"UPDATE empleados 
                         SET nombre_completo = @nom, 
                             telefono = @tel, 
                             usuario = @usu, 
                             id_rol = @rol";

                // 2. Solo actualizamos la contraseña si el usuario escribió una nueva
                if (!string.IsNullOrEmpty(emp.contrasena))
                {
                    query += ", contrasena = @con";
                }

                // 3. Cerramos la consulta con la condición WHERE
                query += " WHERE id_empleado = @id";

                var cmd = new MySqlCommand(query, conn);

                // Pasamos todos los parámetros nuevos
                cmd.Parameters.AddWithValue("@nom", emp.nombre_completo);
                cmd.Parameters.AddWithValue("@tel", emp.telefono);
                cmd.Parameters.AddWithValue("@usu", emp.usuario);
                cmd.Parameters.AddWithValue("@rol", emp.id_rol);
                cmd.Parameters.AddWithValue("@id", emp.id_empleado);

                if (!string.IsNullOrEmpty(emp.contrasena))
                {
                    cmd.Parameters.AddWithValue("@con", emp.contrasena);
                }

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error al actualizar empleado: " + ex.Message, "Error BD", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }

            CargarEmpleados();
        }

        // --- GESTIÓN DE ASIGNACIONES: ELIMINACIÓN POR FECHA ---
        public static void EliminarAsignacionPorFecha(int idEmpleado, DateTime fecha)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // Cambiamos DELETE por UPDATE
                string query = "UPDATE asignaciones SET estatus = 'INACTIVO' WHERE id_empleado = @emp AND DATE(fecha) = DATE(@fec)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emp", idEmpleado);
                cmd.Parameters.AddWithValue("@fec", fecha);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    SincronizarEstatusVistaJefe();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al eliminar asignación: " + ex.Message);
                }
            }
            CargarAsignaciones();
        }
        // --- GESTIÓN DE UBICACIONES: NUEVOS MÉTODOS PARA EL MAPA ---

        public static int CrearUbicacionRetornandoId(Ubicacion u)
        {
            int nuevoId = 0;
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = @"INSERT INTO ubicaciones (nombre_lugar, latitud, longitud, radio_permitido) 
                         VALUES (@nom, @lat, @lng, @rad);
                         SELECT LAST_INSERT_ID();";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                cmd.Parameters.AddWithValue("@lat", u.latitud);
                cmd.Parameters.AddWithValue("@lng", u.longitud);
                cmd.Parameters.AddWithValue("@rad", u.radio_permitido > 0 ? u.radio_permitido : 200);

                try
                {
                    conn.Open();
                    nuevoId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al crear ubicación desde mapa: " + ex.Message);
                    throw;
                }
            }
            CargarUbicaciones();
            return nuevoId;
        }

         public static void EliminarUbicacion(int idUbicacion)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                // Cambiamos DELETE por UPDATE
                string query = "UPDATE ubicaciones SET estatus = 'Inactivo' WHERE id_ubicacion = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idUbicacion);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al eliminar ubicación: " + ex.Message);
                    throw;
                }
            }
            CargarUbicaciones();
        }
        }
}