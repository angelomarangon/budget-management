using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManagement.Models;

public class CuentaCreacionViewModel: Cuenta
{
    public IEnumerable<SelectListItem> TiposCuentas { get; set; }
}