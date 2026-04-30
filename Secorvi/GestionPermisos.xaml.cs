using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel; // Para ICollectionView
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;   // Para CollectionViewSource

namespace Secorvi
{
    public partial class GestionPermisos : Window
    {
        public int IdEmpleadoSeleccionado { get; private set; }
        public int IdRolSeleccionado { get; private set; }
        public string NuevaContrasena { get; private set; }

        private Empleado _emp;
        private ICollectionView _empleadosView; // El motor de búsqueda

        public GestionPermisos()
        {
            InitializeComponent();
            CargarEmpleados();
            CargarRolesManual();
        }

        private void CargarEmpleados()
        {
            var listaEmpleados = DataService.Empleados.OrderBy(e => e.nombre_completo).ToList();

            // Configuramos la vista y el filtro (igual que en el PanelDeControl)
            _empleadosView = CollectionViewSource.GetDefaultView(listaEmpleados);
            _empleadosView.Filter = (obj) =>
            {
                if (obj is Empleado emp)
                {
                    string filtro = txtBuscadorEmpleado.Text?.Trim().ToLower() ?? "";
                    if (string.IsNullOrEmpty(filtro)) return true;

                    return (emp.nombre_completo?.ToLower().Contains(filtro) ?? false) ||
                           emp.id_empleado.ToString().Contains(filtro) ||
                           (emp.matricula?.ToLower().Contains(filtro) ?? false) ||
                           (emp.telefono?.Contains(filtro) ?? false);
                }
                return false;
            };

            lstEmpleadosResultados.ItemsSource = _empleadosView;
        }

        // EVENTO 1: Cuando escribes en la caja, se filtra la lista automáticamente
        private void TxtBuscadorEmpleado_TextChanged(object sender, TextChangedEventArgs e)
        {
            _empleadosView?.Refresh();

            // LÓGICA DE UX: Solo mostramos la lista si hay texto escrito
            bool hayBusqueda = !string.IsNullOrWhiteSpace(txtBuscadorEmpleado.Text);

            lstEmpleadosResultados.Visibility = hayBusqueda ? Visibility.Visible : Visibility.Collapsed;

            // Si la lista está dentro de un borde en el XAML, también lo ocultamos para que no quede una raya flotando
            if (lstEmpleadosResultados.Parent is Border borde)
            {
                borde.Visibility = hayBusqueda ? Visibility.Visible : Visibility.Collapsed;
            }

            // Ocultar configuración hasta que seleccione alguien de los resultados
            panelConfiguracion.Visibility = Visibility.Collapsed;
            btnGuardar.IsEnabled = false;
            lblNombreEmpleado.Text = "SELECCIONE UN AGENTE...";
        }

        // EVENTO 2: Cuando das clic a alguien en la lista
        private void LstEmpleadosResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEmpleadosResultados.SelectedItem is Empleado seleccionado)
            {
                _emp = seleccionado;
                IdEmpleadoSeleccionado = _emp.id_empleado;

                // Actualizamos la etiqueta de la interfaz
                lblNombreEmpleado.Text = _emp.nombre_completo.ToUpper();

                // Revelamos el panel y habilitamos el botón de guardar
                panelConfiguracion.Visibility = Visibility.Visible;
                btnGuardar.IsEnabled = true;

                // Forzamos a que el combo de roles seleccione el actual
                cbRoles.SelectedValue = _emp.id_rol;

                // Limpiamos
                txtNuevaPassUsuario.Clear();
                txtNuevaPassUsuarioVisible.Clear();
                CancelarModoAutorizacion();

                // LÓGICA DE UX: Ocultar la lista de resultados para limpiar la pantalla
                lstEmpleadosResultados.Visibility = Visibility.Collapsed;
                if (lstEmpleadosResultados.Parent is Border borde)
                {
                    borde.Visibility = Visibility.Collapsed;
                }

                // Autocompletamos la caja de texto con el nombre seleccionado para que se vea elegante.
                // Desconectamos el evento temporalmente para que no vuelva a abrir la lista al cambiar el texto.
                txtBuscadorEmpleado.TextChanged -= TxtBuscadorEmpleado_TextChanged;
                txtBuscadorEmpleado.Text = _emp.nombre_completo;
                txtBuscadorEmpleado.TextChanged += TxtBuscadorEmpleado_TextChanged;
            }
        }

        private void CargarRolesManual()
        {
            var listaRoles = new List<RolManual>
            {
                new RolManual { Id = 1, Nombre = "SUPER ADMIN", Desc = "ACCESO TOTAL: Control absoluto del sistema y configuración técnica." },
                new RolManual { Id = 2, Nombre = "SUPERVISOR", Desc = "GESTIÓN OPERATIVA: Supervisa asignaciones, rutas y cumplimiento de agentes en campo." },
                new RolManual { Id = 3, Nombre = "AGENTE", Desc = "PERSONAL DE CAMPO: Registro de asistencias mediante WhatsApp." }
            };
            cbRoles.ItemsSource = listaRoles;
        }

        private void CbRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbRoles.SelectedItem is RolManual selected && _emp != null)
            {
                txtDescripcionRol.Text = selected.Desc;
                IdRolSeleccionado = selected.Id;

                int rolDeseado = selected.Id;

                if (rolDeseado == 1 || rolDeseado == 2)
                {
                    brdContrasenaUsuario.Visibility = Visibility.Visible;

                    if (!string.IsNullOrEmpty(_emp.contrasena) && string.IsNullOrEmpty(txtNuevaPassUsuario.Password))
                    {
                        txtNuevaPassUsuario.Password = _emp.contrasena;
                        txtNuevaPassUsuarioVisible.Text = _emp.contrasena;
                    }
                }
                else
                {
                    brdContrasenaUsuario.Visibility = Visibility.Collapsed;
                }

                if (rolDeseado == 1)
                {
                    txtMensajeAutorizacion.Text = "Se requiere confirmación. Firme abajo usando las credenciales de un SUPER ADMIN actual.";
                    brdAutorizacion.Visibility = Visibility.Visible;
                    ConfigurarBoton(true);
                }
                else if (rolDeseado == 2)
                {
                    txtMensajeAutorizacion.Text = "Se requiere confirmación. Firme abajo usando las credenciales de un ADMIN o SUPERVISOR activo.";
                    brdAutorizacion.Visibility = Visibility.Visible;
                    ConfigurarBoton(true);
                }
                else
                {
                    brdAutorizacion.Visibility = Visibility.Collapsed;
                    ConfigurarBoton(false);
                }
            }
        }

        private void ConfigurarBoton(bool requiereFirma)
        {
            if (requiereFirma)
            {
                btnGuardar.Content = "VERIFICAR Y GUARDAR";
                btnGuardar.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5252"));
                btnGuardar.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                btnGuardar.Content = "APLICAR CAMBIOS";
                btnGuardar.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFB300"));
                btnGuardar.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0B0D12"));
            }
        }

        private void BtnVerPass_Click(object sender, RoutedEventArgs e)
        {
            int rolDeseado = cbRoles.SelectedValue != null ? (int)cbRoles.SelectedValue : 3;

            if (rolDeseado == 1 || _emp.id_rol == 1)
            {
                if (string.IsNullOrWhiteSpace(txtUserAuth.Text) || string.IsNullOrWhiteSpace(txtPassAuth.Password))
                {
                    MessageBox.Show("PROTOCOLO SUPER ADMIN: Para visualizar o modificar esta clave, primero debe autorizar la acción ingresando su Usuario y Clave de Autorizador en la sección inferior.", "ACCESO DENEGADO", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtUserAuth.Focus();
                    return;
                }

                if (!ValidarCredencialesSeguridad(txtUserAuth.Text, txtPassAuth.Password, 1))
                {
                    MessageBox.Show("FIRMA INVÁLIDA: Las credenciales ingresadas abajo no pertenecen a un Super Admin autorizado.", "BRECHA DE SEGURIDAD", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (txtNuevaPassUsuario.Visibility == Visibility.Visible)
            {
                txtNuevaPassUsuarioVisible.Text = txtNuevaPassUsuario.Password;
                txtNuevaPassUsuario.Visibility = Visibility.Collapsed;
                txtNuevaPassUsuarioVisible.Visibility = Visibility.Visible;

                btnVerPass.Content = "🙈";
                lblInfoPass.Text = "Clave expuesta. Mantenga la confidencialidad.";
                lblInfoPass.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 82, 82));
            }
            else
            {
                txtNuevaPassUsuario.Password = txtNuevaPassUsuarioVisible.Text;
                txtNuevaPassUsuarioVisible.Visibility = Visibility.Collapsed;
                txtNuevaPassUsuario.Visibility = Visibility.Visible;

                btnVerPass.Content = "👁️";
                lblInfoPass.Text = "Clave encriptada. Presione el ícono para visualizarla.";
                lblInfoPass.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Verificación básica: ¿Hay un empleado y un rol seleccionados?
            if (_emp == null || cbRoles.SelectedItem == null) return;

            // 2. Extraemos el Rol deseado de la propiedad que se actualiza en el SelectionChanged
            int rolDeseado = IdRolSeleccionado;

            // Obtenemos la clave de texto o de passwordbox dependiendo de cuál esté visible
            string claveUsuario = txtNuevaPassUsuario.Visibility == Visibility.Visible
                                  ? txtNuevaPassUsuario.Password
                                  : txtNuevaPassUsuarioVisible.Text;

            // 3. PROTOCOLO DE SEGURIDAD PARA ADMINISTRADOR (1) Y SUPERVISOR (2)
            if (rolDeseado == 1 || rolDeseado == 2)
            {
                // Validamos que el usuario tenga una clave (obligatorio para estos niveles)
                if (string.IsNullOrWhiteSpace(claveUsuario))
                {
                    MessageBox.Show("DEBE ASIGNAR O CONFIRMAR UNA CONTRASEÑA PARA EL USUARIO.", "DATOS INCOMPLETOS", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validamos que el responsable haya firmado abajo en la sección de autorizador
                if (string.IsNullOrWhiteSpace(txtUserAuth.Text) || string.IsNullOrWhiteSpace(txtPassAuth.Password))
                {
                    MessageBox.Show("DEBE INGRESAR SU USUARIO Y CONTRASEÑA EN LA SECCIÓN DE FIRMA PARA AUTORIZAR ESTE CAMBIO.", "FALTA FIRMA", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ejecutamos la validación técnica de las credenciales del autorizador
                bool validado = ValidarCredencialesSeguridad(txtUserAuth.Text, txtPassAuth.Password, rolDeseado);

                if (!validado)
                {
                    MessageBox.Show("FIRMA INVÁLIDA O NIVEL DE ACCESO INSUFICIENTE.", "BRECHA DE SEGURIDAD", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassAuth.Clear();
                    return;
                }
            }

            // 4. PREPARACIÓN DE RESULTADOS PARA EL PANEL DE CONTROL
            // Si el rol es 3 (Agente), la contraseña viaja como NULL.
            // IMPORTANTE: Recuerda aplicar el cambio en MySQL (ALTER TABLE empleados MODIFY contrasena varchar(100) NULL;)
            IdRolSeleccionado = rolDeseado;
            NuevaContrasena = (rolDeseado == 3) ? null : claveUsuario;

            MessageBox.Show("CAMBIOS APLICADOS CORRECTAMENTE.", "SECORVI SECURITY", MessageBoxButton.OK, MessageBoxImage.Information);

            // Cerramos la ventana devolviendo 'true' para que el Panel de Control refresque la tabla
            this.DialogResult = true;
        }
        private bool ValidarCredencialesSeguridad(string user, string pass, int rolDeseado)
        {
            var authEmpleado = DataService.Empleados.FirstOrDefault(e => e.usuario == user && e.contrasena == pass);

            if (authEmpleado != null)
            {
                if (rolDeseado == 1 && authEmpleado.id_rol != 1) return false;
                if (rolDeseado == 2 && authEmpleado.id_rol > 2) return false;
                return true;
            }
            return false;
        }

        private void CancelarModoAutorizacion()
        {
            txtUserAuth.Clear();
            txtPassAuth.Clear();

            if (txtNuevaPassUsuario.Visibility == Visibility.Collapsed)
            {
                BtnVerPass_Click(null, null);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

    public class RolManual
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Desc { get; set; }
    }
}