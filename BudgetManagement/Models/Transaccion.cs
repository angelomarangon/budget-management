using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BudgetManagement.Models;

public class Transaccion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    [Display(Name = "Fecha de la Transaccion")]
    [DataType(DataType.Date)]
    public DateTime FechaTransaccion { get; set; } = DateTime.Today;
    public decimal Monto { get; set; }
    [Range(0, maximum: int.MaxValue, ErrorMessage = "Debe seleccionar una categoria")]
    [Display(Name = "Categoria")]
    public int CategoriaId { get; set; }
    [StringLength(maximumLength:1000, ErrorMessage = "La nota no puede pasar de {1} caracteres")]
    public string Nota { get; set; }
    [Range(0, maximum: int.MaxValue, ErrorMessage = "Debe seleccionar una cuenta")]
    [Display(Name = "Cuenta")]
    public int CuentaId { get; set; }
    [Display(Name = "Tipo de operacion")]
    public TipoOperacion TipoOperacionId { get; set; } = TipoOperacion.Ingreso;
    public string Cuenta { get; set; }
    public string Categoria { get; set; }
}