using System;
using System.Windows;
using System.Windows.Controls;
using Secorvi.Models;
using System.Linq;

namespace Secorvi
{
    public partial class GestionTurnos : Page
    {
        public GestionTurnos()
        {
            InitializeComponent(); // Ahora debería funcionar
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                DataService.ActualizarTodo();
                cbEmpleados.ItemsSource = DataService.Empleados;
                cbUbicaciones.ItemsSource = DataService.Ubicaciones;
                dgTurnos.ItemsSource = DataService.Asignaciones;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void btnGuardarTurno_Click(object sender, RoutedEventArgs e)
        {
            var emp = (Empleado)cbEmpleados.SelectedItem;
            var ubi = (Ubicacion)cbUbicaciones.SelectedItem;

            if (emp == null || ubi == null || dpFecha.SelectedDate == null)
            {
                MessageBox.Show("SISTEMA: Completa los campos.");
                return;
            }

            DataService.CrearAsignacion(new Asignacion
            {
                id_empleado = emp.id_empleado,
                id_ubicacion = ubi.id_lugar,
                fecha = dpFecha.SelectedDate.Value,
                estatus = "PROGRAMADO"
            });

            CargarDatos();
        }

        private void btnEliminarTurno_Click(object sender, RoutedEventArgs e)
        {
            if (dgTurnos.SelectedItem is Asignacion asig)
            {
                DataService.EliminarAsignacionPorFecha(asig.id_empleado, asig.fecha);
                CargarDatos();
            }
        }
    }
}