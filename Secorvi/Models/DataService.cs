using MySql.Data.MySqlClient;
using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
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
        private static string connectionString = ConfigurationManager.ConnectionStrings["SecorviDB"]?.ConnectionString ?? "Server=svukid.easypanel.host;Port=33060;Database=secorvi-db;Uid=admin;Pwd=admin;SslMode=Disabled;AllowPublicKeyRetrieval=true;";

        public static List<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
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
            var asistenciasHoy = new Dictionary<int, (string estado, TimeSpan? entrada, TimeSpan? salida)>();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
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
                catch { }
            }

            foreach (var emp in Empleados.ToList())
            {
                var asig = Asignaciones.ToList().FirstOrDefault(a => a.id_empleado == emp.id_empleado && a.fecha.Date == DateTime.Today);

                if (asig != null)
                {
                    if (asistenciasHoy.ContainsKey(emp.id_empleado))
                    {
                        var real = asistenciasHoy[emp.id_empleado];
                        emp.estatus_asistencia = real.estado ?? asig.estatus;

                        string horaIn = real.entrada.HasValue ? real.entrada.Value.ToString(@"hh\:mm") : asig.hora_inicio.ToString(@"hh\:mm");
                        string horaOut = real.salida.HasValue ? real.salida.Value.ToString(@"hh\:mm") : asig.hora_fin.ToString(@"hh\:mm");

                        if (real.estado == "SALIDA")
                            emp.info_turno = $"FINALIZADO: {horaIn} - {horaOut}";
                        else
                            emp.info_turno = $"EN CURSO: {horaIn} - {horaOut}";
                    }
                    else
                    {
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
        }

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
                    System.Windows.MessageBox.Show("Error conectando a BD (Empleados): " + ex.Message);
                }
            }
        }

        public static void CargarUbicaciones()
        {
            Ubicaciones.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM ubicaciones WHERE nombre_lugar NOT LIKE '[INACTIVA]%'", conn);
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

        public static void EliminarUbicacion(int idUbicacion)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "UPDATE ubicaciones SET nombre_lugar = CONCAT('[INACTIVA] ', nombre_lugar) WHERE id_ubicacion = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idUbicacion);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al inactivar ubicación: " + ex.Message);
                    throw;
                }
            }
            CargarUbicaciones();
        }

        public static void CargarAsignaciones()
        {
            Asignaciones.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM asignaciones WHERE estatus != 'INACTIVO'", conn);
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
                if (e.Length > 50) e = e.Substring(0, 50);

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
                    string queryHijos = "UPDATE asistencias SET estatus = 'En proceso' WHERE id_asignacion = @id";
                    var cmd1 = new MySqlCommand(queryHijos, conn, transaction);
                    cmd1.Parameters.AddWithValue("@id", idAsignacion);
                    cmd1.ExecuteNonQuery();

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

        // ==========================================
        // AGREGAR EMPLEADO CON VALIDACIÓN HÍBRIDA
        // ==========================================
        public static void AgregarEmpleado(Empleado emp)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Revisamos si alguno de los campos UNIQUE ya existe (Matrícula, Usuario o Teléfono)
                string checkQuery = "SELECT id_empleado, estatus FROM empleados WHERE matricula = @mat OR usuario = @usu OR telefono = @tel LIMIT 1";
                int? idExistente = null;
                string estatusActual = "";

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@mat", emp.matricula);
                    checkCmd.Parameters.AddWithValue("@usu", emp.usuario);
                    checkCmd.Parameters.AddWithValue("@tel", emp.telefono);

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
                    if (estatusActual == "Inactivo")
                    {
                        // Si está inactivo, ofrecemos restaurarlo con los nuevos datos
                        var result = System.Windows.MessageBox.Show(
                            "Se encontró un registro INACTIVO que coincide con la matrícula, teléfono o usuario ingresado.\n\n¿Deseas restaurar al agente y reemplazar su configuración anterior con estos nuevos datos?",
                            "RESTAURACIÓN SUGERIDA",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        if (result == System.Windows.MessageBoxResult.Yes)
                        {
                            string updateQuery = @"UPDATE empleados 
                                                   SET nombre_completo = @nom, telefono = @tel, id_rol = @rol, 
                                                       estatus = 'Activo', usuario = @usu, contrasena = @con, matricula = @mat 
                                                   WHERE id_empleado = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@id", idExistente.Value);
                                updateCmd.Parameters.AddWithValue("@nom", emp.nombre_completo);
                                updateCmd.Parameters.AddWithValue("@tel", emp.telefono);
                                updateCmd.Parameters.AddWithValue("@rol", emp.id_rol);
                                updateCmd.Parameters.AddWithValue("@usu", emp.usuario);
                                updateCmd.Parameters.AddWithValue("@con", emp.contrasena);
                                updateCmd.Parameters.AddWithValue("@mat", emp.matricula);
                                updateCmd.ExecuteNonQuery();
                            }
                            System.Windows.MessageBox.Show("Agente reactivado y actualizado exitosamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Los datos ingresados (Matrícula, Usuario o Teléfono) ya pertenecen a un agente ACTIVO. Por favor verifica la información para no crear duplicados.", "DUPLICADO DETECTADO", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Si no existe ni activo ni inactivo, se hace el INSERT normal
                    string insertQuery = @"INSERT INTO empleados 
                    (id_empleado, nombre_completo, telefono, id_rol, estatus, usuario, contrasena, matricula) 
                    VALUES (@id, @nom, @tel, @rol, 'Activo', @usu, @con, @mat)";

                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@id", ObtenerProximoIdEmpleado());
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
                string queryAsignaciones = "UPDATE asignaciones SET estatus = 'INACTIVO' WHERE id_empleado = @id AND fecha >= CURDATE()";
                using (var cmdAsig = new MySqlCommand(queryAsignaciones, conn))
                {
                    cmdAsig.Parameters.AddWithValue("@id", id);
                    cmdAsig.ExecuteNonQuery();
                }

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

        // ==========================================
        // AGREGAR UBICACIÓN CON VALIDACIÓN HÍBRIDA
        // ==========================================
        public static void AgregarUbicacion(Ubicacion u)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string nombreInactivo = $"[INACTIVA] {u.nombre_lugar}";
                string checkQuery = "SELECT id_ubicacion, nombre_lugar FROM ubicaciones WHERE nombre_lugar = @nom OR nombre_lugar = @nomInactivo LIMIT 1";

                int? idExistente = null;
                bool estaInactiva = false;

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                    checkCmd.Parameters.AddWithValue("@nomInactivo", nombreInactivo);

                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idExistente = Convert.ToInt32(reader["id_ubicacion"]);
                            string nombreDB = reader["nombre_lugar"].ToString();
                            estaInactiva = nombreDB.StartsWith("[INACTIVA]");
                        }
                    }
                }

                if (idExistente.HasValue)
                {
                    string msj = estaInactiva
                        ? $"La zona '{u.nombre_lugar}' fue eliminada anteriormente y se encuentra inactiva.\n\n¿Deseas REACTIVARLA y reemplazarla (SÍ) o CREAR UNA NUEVA versión (NO)?"
                        : $"Ya existe una zona activa llamada '{u.nombre_lugar}'.\n\n¿Deseas REEMPLAZAR sus coordenadas y permisos (SÍ) o CREAR UNA NUEVA VERSIÓN (NO)?";

                    var result = System.Windows.MessageBox.Show(msj, "DUPLICADO DETECTADO",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        string updateQuery = @"UPDATE ubicaciones 
                                       SET nombre_lugar = @nom, latitud = @lat, longitud = @lng, radio_permitido = @rad 
                                       WHERE id_ubicacion = @id";
                        using (var updateCmd = new MySqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", idExistente.Value);
                            updateCmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                            updateCmd.Parameters.AddWithValue("@lat", u.latitud);
                            updateCmd.Parameters.AddWithValue("@lng", u.longitud);
                            updateCmd.Parameters.AddWithValue("@rad", u.radio_permitido);
                            updateCmd.ExecuteNonQuery();
                        }
                        System.Windows.MessageBox.Show("Zona reemplazada exitosamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else if (result == System.Windows.MessageBoxResult.No)
                    {
                        string nuevoNombre = ObtenerSiguienteNombreVolumen(conn, u.nombre_lugar);
                        InsertarNuevaUbicacion(conn, nuevoNombre, u.latitud, u.longitud, u.radio_permitido);
                        System.Windows.MessageBox.Show($"Se ha creado la nueva zona: {nuevoNombre}", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
                else
                {
                    InsertarNuevaUbicacion(conn, u.nombre_lugar, u.latitud, u.longitud, u.radio_permitido);
                }
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
                    string query = "DELETE FROM ubicaciones WHERE id_ubicacion = @id";
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
                string query = @"UPDATE empleados 
                                 SET nombre_completo = @nom, 
                                     telefono = @tel, 
                                     usuario = @usu, 
                                     id_rol = @rol";

                if (!string.IsNullOrEmpty(emp.contrasena))
                {
                    query += ", contrasena = @con";
                }

                query += " WHERE id_empleado = @id";

                var cmd = new MySqlCommand(query, conn);

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

        public static void EliminarAsignacionPorFecha(int idEmpleado, DateTime fecha)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
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

        // ==========================================
        // CREAR UBICACIÓN CON VALIDACIÓN HÍBRIDA (MAPA)
        // ==========================================
        public static int CrearUbicacionRetornandoId(Ubicacion u)
        {
            int nuevoId = 0;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string nombreInactivo = $"[INACTIVA] {u.nombre_lugar}";
                string checkQuery = "SELECT id_ubicacion, nombre_lugar FROM ubicaciones WHERE nombre_lugar = @nom OR nombre_lugar = @nomInactivo LIMIT 1";

                int? idExistente = null;
                bool estaInactiva = false;

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                    checkCmd.Parameters.AddWithValue("@nomInactivo", nombreInactivo);

                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idExistente = Convert.ToInt32(reader["id_ubicacion"]);
                            string nombreDB = reader["nombre_lugar"].ToString();
                            estaInactiva = nombreDB.StartsWith("[INACTIVA]");
                        }
                    }
                }

                if (idExistente.HasValue)
                {
                    string titulo = estaInactiva ? "RESTAURACIÓN SUGERIDA" : "DUPLICADO DETECTADO";
                    string msj = estaInactiva
                        ? $"La zona '{u.nombre_lugar}' existe pero está oculta/inactiva.\n\n¿Deseas REACTIVARLA y reemplazar sus coordenadas (SÍ) o CREAR UNA NUEVA VERSIÓN (NO)?"
                        : $"Ya existe una zona activa llamada '{u.nombre_lugar}'.\n\n¿Deseas REEMPLAZAR sus coordenadas (SÍ) o CREAR UNA NUEVA VERSIÓN agregando un número de volumen (NO)?";

                    var result = System.Windows.MessageBox.Show(msj, titulo,
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        // REEMPLAZAR (UPDATE)
                        string updateQuery = @"UPDATE ubicaciones 
                                       SET nombre_lugar = @nom, latitud = @lat, longitud = @lng, radio_permitido = @rad 
                                       WHERE id_ubicacion = @id";
                        using (var updateCmd = new MySqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", idExistente.Value);
                            updateCmd.Parameters.AddWithValue("@nom", u.nombre_lugar);
                            updateCmd.Parameters.AddWithValue("@lat", u.latitud);
                            updateCmd.Parameters.AddWithValue("@lng", u.longitud);
                            updateCmd.Parameters.AddWithValue("@rad", u.radio_permitido > 0 ? u.radio_permitido : 200);
                            updateCmd.ExecuteNonQuery();
                        }
                        nuevoId = idExistente.Value;
                    }
                    else if (result == System.Windows.MessageBoxResult.No)
                    {
                        // AGREGAR NUEVO (VOLUMEN AUTOMÁTICO)
                        string nuevoNombre = ObtenerSiguienteNombreVolumen(conn, u.nombre_lugar);
                        nuevoId = InsertarNuevaUbicacion(conn, nuevoNombre, u.latitud, u.longitud, u.radio_permitido);
                    }
                    else
                    {
                        // CANCELAR
                        return 0;
                    }
                }
                else
                {
                    // CREACIÓN NORMAL PORQUE NO EXISTE
                    nuevoId = InsertarNuevaUbicacion(conn, u.nombre_lugar, u.latitud, u.longitud, u.radio_permitido);
                }
            }
            CargarUbicaciones();
            return nuevoId;
        }

        public static List<Empleado> ObtenerEmpleadosInactivos()
        {
            var inactivos = new List<Empleado>();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM empleados WHERE estatus = 'Inactivo'", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            inactivos.Add(new Empleado
                            {
                                id_empleado = Convert.ToInt32(r["id_empleado"]),
                                nombre_completo = r["nombre_completo"].ToString()
                            });
                        }
                    }
                }
                catch { }
            }
            return inactivos;
        }

        public static List<Ubicacion> ObtenerUbicacionesInactivas()
        {
            var inactivos = new List<Ubicacion>();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT * FROM ubicaciones WHERE nombre_lugar LIKE '[INACTIVA] %'", conn);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            inactivos.Add(new Ubicacion
                            {
                                id_ubicacion = Convert.ToInt32(r["id_ubicacion"]),
                                nombre_lugar = r["nombre_lugar"].ToString()
                            });
                        }
                    }
                }
                catch { }
            }
            return inactivos;
        }

        public static void ReactivarEmpleado(int idEmpleado)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "UPDATE empleados SET estatus = 'Activo' WHERE id_empleado = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idEmpleado);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarEmpleados();
        }
        private static string ObtenerSiguienteNombreVolumen(MySqlConnection conn, string nombreBase)
        {
            int maxVol = 1;
            string prefixBusqueda = nombreBase + " vol ";
            string query = "SELECT nombre_lugar FROM ubicaciones WHERE nombre_lugar LIKE @likeStr";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@likeStr", nombreBase + "%");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string n = reader["nombre_lugar"].ToString();
                        n = n.Replace("[INACTIVA] ", ""); // Ignorar etiqueta de inactividad

                        if (n.Equals(nombreBase, StringComparison.OrdinalIgnoreCase))
                        {
                            maxVol = Math.Max(maxVol, 1);
                        }
                        else if (n.StartsWith(prefixBusqueda, StringComparison.OrdinalIgnoreCase))
                        {
                            string numStr = n.Substring(prefixBusqueda.Length).Trim();
                            if (int.TryParse(numStr, out int v))
                            {
                                maxVol = Math.Max(maxVol, v);
                            }
                        }
                    }
                }
            }
            return $"{nombreBase} vol {maxVol + 1}";
        }

        private static int InsertarNuevaUbicacion(MySqlConnection conn, string nombre, decimal lat, decimal lng, int rad)
        {
            string insertQuery = @"INSERT INTO ubicaciones (nombre_lugar, latitud, longitud, radio_permitido) 
                           VALUES (@nom, @lat, @lng, @rad);
                           SELECT LAST_INSERT_ID();";
            using (var cmd = new MySqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@nom", nombre);
                cmd.Parameters.AddWithValue("@lat", lat);
                cmd.Parameters.AddWithValue("@lng", lng);
                cmd.Parameters.AddWithValue("@rad", rad > 0 ? rad : 200);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public static void ReactivarUbicacion(int idUbicacion)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                string query = "UPDATE ubicaciones SET nombre_lugar = REPLACE(nombre_lugar, '[INACTIVA] ', '') WHERE id_ubicacion = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idUbicacion);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            CargarUbicaciones();
        }
    }
}