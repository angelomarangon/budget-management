using BudgetManagement.Models;

namespace BudgetManagement.Services;

public interface IServicioReportes
{
    Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladasPorCuenta(
        int usuarioId,
        int cuentaId,
        int mes,
        int anio,
        dynamic ViewBag);

    Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladas(
        int usuarioId, 
        int mes,
        int anio, 
        dynamic ViewBag);

    Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerReporteSemanal(int usuarioId, int mes, int anio,
        dynamic ViewBag);
}

public class ServicioReportes : IServicioReportes
{
    private readonly IRepositorioTransacciones _repositorioTransacciones;
    private readonly HttpContext httpContext;

    public ServicioReportes(IRepositorioTransacciones repositorioTransacciones, IHttpContextAccessor httpContextAccessor)
    {
        _repositorioTransacciones = repositorioTransacciones;
        httpContext = httpContextAccessor.HttpContext;
    }

    public async Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladas(
        int usuarioId, 
        int mes,
        int anio, 
        dynamic ViewBag)
    {
        (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, anio);
        
        var parametro = new ParametroObtenerTransaccionesPorUsuario()
        {
            UsuarioId = usuarioId,
            FechaInicio = fechaInicio,
            Fechafin = fechaFin,
        };
        
        var transacciones = await _repositorioTransacciones.ObtenerPorUsuarioId(parametro);
        
        var modelo = GenerarReporteTransaccionesDetalladas(transacciones, fechaInicio, fechaFin);
        
        AsignarValoresAlViewBag(ViewBag, fechaInicio);
        
        return modelo;
    }

    public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerReporteSemanal(int usuarioId, int mes, int anio,
        dynamic ViewBag)
    {
        (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, anio);
        
        var parametro = new ParametroObtenerTransaccionesPorUsuario()
        {
            UsuarioId = usuarioId,
            FechaInicio = fechaInicio,
            Fechafin = fechaFin,
        };
        
        AsignarValoresAlViewBag(ViewBag, fechaInicio);
        var modelo = await _repositorioTransacciones.ObtenerPorSemana(parametro);
        return modelo;
    }
    
    public async Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladasPorCuenta(
        int usuarioId,
        int cuentaId,
        int mes,
        int anio,
        dynamic ViewBag)
    {
        (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, anio);
        
        var obtenerTransaccionesPorCuenta = new ObtenerTransaccionesPorCuenta()
        {
            CuentaId = cuentaId,
            UsuarioId = usuarioId,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
        };
        
        var transacciones = await _repositorioTransacciones.ObtenerPorCuentaId(obtenerTransaccionesPorCuenta);
        
        var modelo = GenerarReporteTransaccionesDetalladas(transacciones, fechaInicio, fechaFin);

        AsignarValoresAlViewBag(ViewBag, fechaInicio);

        return modelo;
    }

    private void AsignarValoresAlViewBag(dynamic ViewBag, DateTime fechaInicio)
    {
        ViewBag.mesAnterior = fechaInicio.AddMonths(-1).Month;
        ViewBag.anioAnterior = fechaInicio.AddMonths(-1).Year;
        ViewBag.mesPosterior = fechaInicio.AddMonths(1).Month;
        ViewBag.anioPosterior = fechaInicio.AddMonths(1).Year;
        ViewBag.urlRetorno = httpContext.Request.Path + httpContext.Request.QueryString;
    }

    private static ReporteTransaccionesDetalladas GenerarReporteTransaccionesDetalladas(IEnumerable<Transaccion> transacciones,
        DateTime fechaInicio, DateTime fechaFin)
    {
        var modelo = new ReporteTransaccionesDetalladas();
        
        var transaccionesPorFechas = transacciones.OrderByDescending(x => x.FechaTransaccion)
            .GroupBy(x => x.FechaTransaccion)
            .Select(grupo => new ReporteTransaccionesDetalladas.TransaccionesPorFecha()
            {
                FechaTransaccion = grupo.Key,
                Transacciones = grupo.AsEnumerable()
            });
        
        modelo.TransaccionesAgrupadas = transaccionesPorFechas;
        modelo.FechaInicio = fechaInicio;
        modelo.FechaFin = fechaFin;
        return modelo;
    }

    private (DateTime fechaInicio, DateTime fechaFin) GenerarFechaInicioYFin(int mes, int anio)
    {
        DateTime fechaInicio;
        DateTime fechaFin;

        if (mes <= 0 || mes > 12 || anio <= 1900)
        {
            var hoy = DateTime.Today;
            fechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
        }
        else
        {
            fechaInicio = new DateTime(anio, mes, 1);
        }
        
        fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
        
        return (fechaInicio, fechaFin);
    }
}