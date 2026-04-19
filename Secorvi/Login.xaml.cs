using Secorvi.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Secorvi
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            InicializarDatos();
        }

        private void InicializarDatos()
        {
            try
            {
                // Esto carga la lista estática en DataService
                DataService.ActualizarTodo();

                if (DataService.Empleados.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("LOG: La base de datos está vacía o no hay conexión.");
                }
            }
            catch (Exception ex)
            {
                MostrarAviso("ERROR DE CONEXIÓN: " + ex.Message, "#F8D7DA", "#721C24");
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string userDigitado = txtUser.Text.Trim();
            string passDigitada = txtPass.Password.Trim();

            if (string.IsNullOrEmpty(userDigitado) || string.IsNullOrEmpty(passDigitada))
            {
                MostrarAviso("CRITICAL: Ingrese usuario y contraseña", "#FFF3CD", "#856404");
                return;
            }

            // BUSQUEDA: Usamos las propiedades en minúsculas (id_empleado, usuario, contrasena, estatus)
            // tal como están en tus nuevos Models sincronizados con SQL.
            var encontrado = DataService.Empleados.FirstOrDefault(x =>
                x.usuario.Equals(userDigitado, StringComparison.OrdinalIgnoreCase) &&
                x.contrasena == passDigitada);

            if (encontrado != null)
            {
                // Validamos el ENUM de la base de datos ('Activo')
                if (!encontrado.estatus.Equals("Activo", StringComparison.OrdinalIgnoreCase))
                {
                    MostrarAviso("ACCESO DENEGADO: Usuario inactivo", "#F8D7DA", "#721C24");
                    return;
                }

                // --- INICIO DE SESIÓN ---
                SesionActual.Usuario = encontrado;

                // Verificamos el rol según los IDs de tu script:
                // 1 = Super Admin, 2 = Admin Empleados, 3 = Agente
                if (encontrado.id_rol == 1 || encontrado.id_rol == 2)
                {
                    System.Diagnostics.Debug.WriteLine($"LOG: Acceso concedido a {encontrado.nombre_completo} con Rol ID: {encontrado.id_rol}");

                    ContenedorPrincipal principal = new ContenedorPrincipal();
                    principal.Show();
                    this.Close();
                }
                else
                {
                    // Si es un Agente (Rol 3), denegamos acceso al panel administrativo de escritorio
                    MessageBox.Show("ACCESO DENEGADO: Tu rol de Agente solo permite uso de Bot de WhatsApp. Contacta al administrador.", "SEGURIDAD SECORVI");
                }
            }
            else
            {
                MostrarAviso("ACCESO DENEGADO: Credenciales incorrectas", "#F8D7DA", "#721C24");
            }
        }

        private void MostrarAviso(string mensaje, string colorFondo, string colorTexto)
        {
            var bc = new BrushConverter();
            if (brdStatus != null)
            {
                brdStatus.Background = (Brush)bc.ConvertFrom(colorFondo);
                txtStatusMsg.Text = mensaje.ToUpper();
                txtStatusMsg.Foreground = (Brush)bc.ConvertFrom(colorTexto);
                brdStatus.Visibility = Visibility.Visible;
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}