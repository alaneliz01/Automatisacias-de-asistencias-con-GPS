using System;
// Esta clase representa la asignación de un empleado a una ubicación y turno específico en un día de la semana determinado. 
namespace Secorvi.Models
{
    public class Asignacion
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public int UbicacionId { get; set; }
        public int TurnoId { get; set; }
        public DayOfWeek DiaSemana { get; set; }
    }
}