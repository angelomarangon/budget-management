namespace BudgetManagement.Models;

public class ReporteMensualViewModel
{
    public IEnumerable<ResultadoObtenerPorMes> TransaccionesPorMes { get; set; }
    public decimal Ingresos => TransaccionesPorMes.Sum(x => x.Ingresos);
    public decimal Gastos => TransaccionesPorMes.Sum(x => x.Gastos);
    public decimal Total => Ingresos + Gastos;
    public int Anio { get; set; }
}