using System.ComponentModel.DataAnnotations;

namespace BudgetManagement.Models;

public class OlvideMiPasswordViewModel
{
    [Required(ErrorMessage = "El campo {0} es requerido")]
    [EmailAddress(ErrorMessage = "El campo debe ser un correo electronico valido")]
    public string Email { get; set; }
}