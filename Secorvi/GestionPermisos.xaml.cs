using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Secorvi
{
    public partial class GestionPermisos : Window
    {
        public int IdEmpleadoSeleccionado { get; private set; }
        public int IdRolSeleccionado { get; private set; }
        public string NuevaContrasena { get; private set; }

        private Empleado _emp;
        private ICollectionView _empleadosView;

        public GestionPermisos()
        {
            InitializeComponent();
            CargarEmpleados();
            CargarRolesManual();
        }

        private void CargarEmpleados()
        {
            var listaEmpleados = DataService.Empleados.OrderBy(e => e.nombre_completo).ToList();

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

        private void TxtBuscadorEmpleado_TextChanged(object sender, TextChangedEventArgs e)
        {
            _empleadosView?.Refresh();

            bool hayBusqueda = !string.IsNullOrWhiteSpace(txtBuscadorEmpleado.Text);

            lstEmpleadosResultados.Visibility = hayBusqueda ? Visibility.Visible : Visibility.Collapsed;

            if (lstEmpleadosResultados.Parent is Border borde)
            {
                borde.Visibility = hayBusqueda ? Visibility.Visible : Visibility.Collapsed;
            }

            panelConfiguracion.Visibility = Visibility.Collapsed;
            btnGuardar.IsEnabled = false;
            lblNombreEmpleado.Text = "SELECCIONE UN AGENTE...";
        }

        private void LstEmpleadosResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEmpleadosResultados.SelectedItem is Empleado seleccionado)
            {
                _emp = seleccionado;
                IdEmpleadoSeleccionado = _emp.id_empleado;

                lblNombreEmpleado.Text = _emp.nombre_completo.ToUpper();

                panelConfiguracion.Visibility = Visibility.Visible;
                btnGuardar.IsEnabled = true;

                cbRoles.SelectedValue = _emp.id_rol;

                txtNuevaPassUsuario.Clear();
                txtNuevaPassUsuarioVisible.Clear();
                CancelarModoAutorizacion();

                lstEmpleadosResultados.Visibility = Visibility.Collapsed;
                if (lstEmpleadosResultados.Parent is Border borde)
                {
                    borde.Visibility = Visibility.Collapsed;
                }

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
                // NUEVO: Candado de seguridad UX para evitar degradar al Super Admin
                if (_emp.id_rol == 1 && selected.Id > 1)
                {
                    MessageBox.Show("PROTOCOLO DE SEGURIDAD: No está permitido degradar el rango de un SUPER ADMIN desde esta interfaz.", "OPERACIÓN DENEGADA", MessageBoxButton.OK, MessageBoxImage.Stop);
                    cbRoles.SelectedValue = 1; // Revertimos el control de la interfaz de inmediato
                    return;
                }

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
            if (_emp == null || cbRoles.SelectedItem == null) return;

            int rolDeseado = IdRolSeleccionado;

            // NUEVO: Candado lógico de backend por si el sistema llega a fallar en la interfaz
            if (_emp.id_rol == 1 && rolDeseado > 1)
            {
                MessageBox.Show("ERROR CRÍTICO: Imposible degradar cuenta de nivel 1. Operación abortada.", "SEGURIDAD DE NÚCLEO", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string claveUsuario = txtNuevaPassUsuario.Visibility == Visibility.Visible
                                  ? txtNuevaPassUsuario.Password
                                  : txtNuevaPassUsuarioVisible.Text;

            if (rolDeseado == 1 || rolDeseado == 2)
            {
                if (string.IsNullOrWhiteSpace(claveUsuario))
                {
                    MessageBox.Show("DEBE ASIGNAR O CONFIRMAR UNA CONTRASEÑA PARA EL USUARIO.", "DATOS INCOMPLETOS", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUserAuth.Text) || string.IsNullOrWhiteSpace(txtPassAuth.Password))
                {
                    MessageBox.Show("DEBE INGRESAR SU USUARIO Y CONTRASEÑA EN LA SECCIÓN DE FIRMA PARA AUTORIZAR ESTE CAMBIO.", "FALTA FIRMA", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool validado = ValidarCredencialesSeguridad(txtUserAuth.Text, txtPassAuth.Password, rolDeseado);

                if (!validado)
                {
                    MessageBox.Show("FIRMA INVÁLIDA O NIVEL DE ACCESO INSUFICIENTE.", "BRECHA DE SEGURIDAD", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassAuth.Clear();
                    return;
                }
            }

            IdRolSeleccionado = rolDeseado;
            NuevaContrasena = (rolDeseado == 3) ? null : claveUsuario;

            MessageBox.Show("CAMBIOS APLICADOS CORRECTAMENTE.", "SECORVI SECURITY", MessageBoxButton.OK, MessageBoxImage.Information);

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