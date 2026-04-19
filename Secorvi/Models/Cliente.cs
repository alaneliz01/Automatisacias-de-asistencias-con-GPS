using System;
using System.Collections.Generic;
using System.Text;

namespace Secorvi.Models
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Ubicacion { get; set; }
    }
}