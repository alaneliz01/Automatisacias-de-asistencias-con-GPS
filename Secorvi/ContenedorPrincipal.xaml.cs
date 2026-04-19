using System.Windows;

namespace Secorvi
{
    public partial class ContenedorPrincipal : Window
    {
        public ContenedorPrincipal()
        {
            InitializeComponent();
            // Esto carga tu diseño industrial tipo web de inmediato
            MainFrame.Navigate(new PanelDeControl());

        }
        private void BtnAgentes_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PanelDeControl());
        }
    }
}