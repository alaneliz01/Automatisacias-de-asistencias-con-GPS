using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Secorvi.Models;
using System.Globalization;
using Microsoft.VisualBasic;

namespace Secorvi
{
    public partial class PanelDeControl : Page
    {
        private List<Empleado> _empleadosCache = new List<Empleado>();
        private const string ADMIN_PASSWORD = "admin123";

        public PanelDeControl()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                txtTarifaGlobal.Text = DataService.TarifaHoraGlobal.ToString();
                CargarDatos();
            };
        }

        private void CargarDatos()
        {
            DataService.CargarTodo();
            _empleadosCache = DataService.CargarEmpleados();
            ActualizarTodo();
        }

        private void BtnActualizarTarifa_Click(object sender, RoutedEventArgs e)
        {
            string inputPass = Interaction.InputBox("Ingrese contraseña de administrador:", "Seguridad", "");

            if (inputPass == ADMIN_PASSWORD)
            {
                if (decimal.TryParse(txtTarifaGlobal.Text, out decimal nuevaTarifa))
                {
                    DataService.TarifaHoraGlobal = nuevaTarifa;
                    DataService.GuardarTodo();
                    ActualizarTodo();
                    MessageBox.Show("Tarifa global actualizada.", "Éxito");
                }
                else
                {
                    MessageBox.Show("Monto no válido.");
                    txtTarifaGlobal.Text = DataService.TarifaHoraGlobal.ToString();
                }
            }
            else if (!string.IsNullOrEmpty(inputPass))
            {
                MessageBox.Show("Acceso denegado.");
                txtTarifaGlobal.Text = DataService.TarifaHoraGlobal.ToString();
            }
        }

        private void ActualizarTodo()
        {
            foreach (var emp in _empleadosCache)
            {
                emp.TotalSueldo = DataService.CalcularSueldoEmpleado(emp);
            }
            FiltrarYMostrar();
        }

        private void FiltrarYMostrar()
        {
            string filtro = txtBusqueda.Text.ToLower().Trim();

            var listaFiltrada = _empleadosCache.Where(emp =>
                emp.Nombre.ToLower().Contains(filtro) ||
                (emp.Telefono != null && emp.Telefono.Contains(filtro)) ||
                emp.Matricula.ToLower().Contains(filtro)
            ).ToList();

            dgEmpleados.ItemsSource = null;
            dgEmpleados.ItemsSource = listaFiltrada;

            decimal subtotalNominas = listaFiltrada.Sum(x => x.TotalSueldo);

            if (decimal.TryParse(txtMontoGasto.Text, out decimal montoGastos))
            {
                decimal granTotal = subtotalNominas + montoGastos;
                txtSubtotal.Text = granTotal.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
            }
            else
            {
                txtSubtotal.Text = subtotalNominas.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
            }
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e) => FiltrarYMostrar();

        private void TxtMontoGasto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSubtotal != null) FiltrarYMostrar();
        }

        private void BtnMapa_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Mapa());
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            int proximoId = _empleadosCache.Count > 0 ? _empleadosCache.Max(emp => emp.Id) + 1 : 1;
            RegistroEmpleado win = new RegistroEmpleado(proximoId);
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true) CargarDatos();
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado seleccionado)
            {
                MessageBoxResult res = MessageBox.Show(
                    $"¿Está seguro de que desea dar de baja definitiva al agente {seleccionado.Nombre}?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (res == MessageBoxResult.Yes)
                {
                    DataService.EliminarEmpleado(seleccionado.Matricula);
                    CargarDatos();
                    MessageBox.Show("Agente eliminado correctamente.", "Sistema");
                }
            }
            else
            {
                MessageBox.Show("Por favor, seleccione un agente de la tabla primero.", "Aviso");
            }
        }

        // MÉTODO ACTUALIZADO CON CALENDARIO
        private void BtnDiaLibre_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpleados.SelectedItem is Empleado seleccionado)
            {
                // 1. Abrir tu ventana de calendario
                SeleccionarFechaLibre win = new SeleccionarFechaLibre();
                win.Owner = Window.GetWindow(this);

                if (win.ShowDialog() == true)
                {
                    // 2. Obtener la fecha seleccionada de la ventana
                    DateTime fecha = win.FechaSeleccionada;

                    // 3. Convertir la fecha al nombre del día (sin tildes para que coincida con el DataService)
                    string diaSemana = fecha.ToString("dddd", new CultureInfo("es-MX"))
                                            .ToLower()
                                            .Replace("á", "a")
                                            .Replace("é", "e")
                                            .Replace("í", "i")
                                            .Replace("ó", "o")
                                            .Replace("ú", "u");

                    // 4. Aplicar el cambio en el servicio de datos
                    DataService.AsignarDiaLibre(seleccionado.Matricula, diaSemana);

                    // 5. Recargar la tabla
                    CargarDatos();

                    MessageBox.Show($"Día {diaSemana.ToUpper()} marcado como LIBRE para {seleccionado.Nombre}.", "Éxito");
                }
            }
            else
            {
                MessageBox.Show("Debe seleccionar un agente de la tabla para asignar un descanso.", "Aviso");
            }
        }
    }
}