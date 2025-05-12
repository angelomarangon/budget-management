using System.ComponentModel.DataAnnotations;

namespace BudgetManagement.Models;

public class RegistroViewModel
{
    [Required(ErrorMessage = "El campo {0} es requerido")]
    [EmailAddress(ErrorMessage = "Debe ser un {0} valido")]
    public string Email { get; set; }
    [Required(ErrorMessage = "El campo {0} es requerido")]
    public string Password { get; set; }
}