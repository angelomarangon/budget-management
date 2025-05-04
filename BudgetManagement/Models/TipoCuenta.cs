
using System.ComponentModel.DataAnnotations;
using BudgetManagement.Validations;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManagement.Models;

public class TipoCuenta
{
    public int Id { get; set; }
    [Required(ErrorMessage = "{0} es requerido")]
    [PrimeraLetraMayuscula]
    [StringLength(maximumLength: 50, MinimumLength = 3, ErrorMessage = "Nombre debe tener entre 3 y 50 caracteres")]
    [Remote(action: "VerificarExisteTipoCuenta", controller:"TiposCuentas")]
    public string Nombre { get; set; }
    public int UsuarioId { get; set; }
    public int Orden { get; set; }
    
   
}