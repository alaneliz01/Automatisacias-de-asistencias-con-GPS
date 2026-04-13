using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
// esto es el contenedor principal, el cual se muestra después del login exitoso, y desde el cual se navega a las diferentes páginas como el panel de control y el mapa
namespace Secorvi
{

    public partial class ContenedorPrincipal : Window
    {
        public ContenedorPrincipal()
        {
            InitializeComponent();
        }
    }
}
