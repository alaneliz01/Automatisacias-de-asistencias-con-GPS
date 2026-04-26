using Secorvi.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Secorvi
{
    public partial class GestionPermisos : Window
    {
        public int IdRolSeleccionado { get; private set; }
        private Empleado _emp;

        public GestionPermisos(Empleado emp)
        {
            InitializeComponent();
            _emp = emp;

            // Nombre en mayúsculas para mantener estética industrial
            lblNombreEmpleado.Text = emp.nombre_completo.ToUpper();
            CargarRolesManual();
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
            cbRoles.DisplayMemberPath = "Nombre";
            cbRoles.SelectedValuePath = "Id";
            cbRoles.SelectedValue = _emp.id_rol;
        }

        private void CbRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbRoles.SelectedItem is RolManual selected)
            {
                txtDescripcionRol.Text = selected.Desc;
                IdRolSeleccionado = selected.Id;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cbRoles.SelectedValue != null)
            {
                IdRolSeleccionado = (int)cbRoles.SelectedValue;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("POR FAVOR SELECCIONE UN NIVEL DE ACCESO.", "OPERACIÓN REQUERIDA");
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