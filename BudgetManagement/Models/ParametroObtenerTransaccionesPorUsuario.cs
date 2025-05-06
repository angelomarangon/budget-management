namespace BudgetManagement.Models;

public class ParametroObtenerTransaccionesPorUsuario
{
    public int UsuarioId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime Fechafin { get; set; }
}