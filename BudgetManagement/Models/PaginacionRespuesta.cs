namespace BudgetManagement.Models;

public class PaginacionRespuesta
{
    public int Pagina { get; set; } = 1;
    public int RecordsPorPagina { get; set; } = 5;
    public int CantidadTotalRecords { get; set; }
    // 100 / 5 => 20paginas 
    public int CantidadTotalDePaginas => (int)Math.Ceiling((double)CantidadTotalRecords / RecordsPorPagina);
    public string BaseURL { get; set; }
}

public class PaginacionRespuesta<T>: PaginacionRespuesta
{
    public IEnumerable<T> Elementos { get; set; }
}