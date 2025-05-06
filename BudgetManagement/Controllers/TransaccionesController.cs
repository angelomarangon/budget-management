using System.Runtime.InteropServices.JavaScript;
using AutoMapper;
using BudgetManagement.Models;
using BudgetManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManagement.Controllers;

public class TransaccionesController : Controller
{
    private readonly IServicioUsuarios _servicioUsuarios;
    private readonly IRepositorioTransacciones _repositorioTransacciones;
    private readonly IRepositorioCuentas _repositorioCuentas;
    private readonly IRepositorioCategorias _repositorioCategorias;
    private readonly IServicioReportes _servicioReportes;
    private readonly IMapper _mapper;

    public TransaccionesController(
        IServicioUsuarios servicioUsuarios,
        IRepositorioTransacciones repositorioTransacciones,
        IRepositorioCuentas repositorioCuentas,
        IRepositorioCategorias repositorioCategorias,
        IServicioReportes servicioReportes,
        IMapper mapper)
    {
        _servicioUsuarios = servicioUsuarios;
        _repositorioTransacciones = repositorioTransacciones;
        _repositorioCuentas = repositorioCuentas;
        _repositorioCategorias = repositorioCategorias;
        _servicioReportes = servicioReportes;
        _mapper = mapper;
    }

    public async Task<IActionResult> Index(int mes, int anio)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        var modelo = await _servicioReportes.ObtenerReporteTransaccionesDetalladas(usuarioId, mes, anio, ViewBag);
        
        return View(modelo);
    }

    public async Task<IActionResult> Semanal(int mes, int anio)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana = 
            await _servicioReportes.ObtenerReporteSemanal(usuarioId, mes, anio, ViewBag);
        var agrupado = transaccionesPorSemana
            .GroupBy(x => x.Semana)
            .Select(x => new ResultadoObtenerPorSemana()
            {
                Semana = x.Key,
                Ingresos = x.Where(x => 
                    x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gastos = x.Where(x => 
                    x.TipoOperacionId == TipoOperacion.Gasto).Select(x => x.Monto).FirstOrDefault()
            }).ToList();

        if (anio == 0 || mes == 0)
        {
            var hoy = DateTime.Today;
            mes = hoy.Month;
            anio = hoy.Year;
        }
        
        var fechaReferencia = new DateTime(anio, mes, 1);
        var diasDelMes = Enumerable.Range(1, fechaReferencia.AddMonths(1).AddDays(-1).Day);

        var diasSegmentados = diasDelMes.Chunk(7).ToList();

        for (int i = 0; i < diasSegmentados.Count(); i++)
        {
            var semana = i + 1;
            var fechaInicio = new DateTime(anio, mes,diasSegmentados[i].First());
            var fechaFin = new DateTime(anio, mes,diasSegmentados[i].Last());
            var grupoSemana = agrupado.FirstOrDefault(x => x.Semana == semana);

            if (grupoSemana is null)
            {
                agrupado.Add(new ResultadoObtenerPorSemana()
                {
                    Semana = semana,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                });
            }
            else
            {
                grupoSemana.FechaInicio = fechaInicio;
                grupoSemana.FechaFin = fechaFin;
            }
        }
        
        agrupado = agrupado.OrderByDescending(x => x.Semana).ToList();

        var modelo = new ReporteSemanalViewModel();
        modelo.TransaccionesPorSemana = agrupado;
        modelo.FechaReferencia = fechaReferencia;
        
        return View(modelo);
    }
    
    public IActionResult Mensual()
    {
        return View();
    }
    
    public IActionResult ExcelReporte()
    {
        return View();
    }
    
    public IActionResult Calendario()
    {
        return View();
    }
    
    public async Task<IActionResult> Crear()
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var modelo = new TransaccionCreacionViewModel();

        modelo.Cuentas = await ObtenerCuentas(usuarioId);
        modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);

        return View(modelo);
    }


    private async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
    {
        var cuentas = await _repositorioCuentas.Buscar(usuarioId);
        return cuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
    }

    [HttpPost]
    public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacio)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

        var categorias = await ObtenerCategorias(usuarioId, tipoOperacio);
        return Ok(categorias);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(TransaccionCreacionViewModel modelo)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

        if (!ModelState.IsValid)
        {
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
            return View(modelo);
        }

        var cuenta = await _repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);
        if (cuenta is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        var categoria = await _repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
        if (categoria is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        modelo.UsuarioId = usuarioId;
        if (modelo.TipoOperacionId == TipoOperacion.Gasto)
        {
            modelo.Monto *= -1;
        }

        await _repositorioTransacciones.Crear(modelo);
        return RedirectToAction("Index");
    }


    [HttpGet]
    public async Task<IActionResult> Editar(int id, string urlRetorno = null)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var transaccion = await _repositorioTransacciones.ObtenerPorId(id, usuarioId);
        if (transaccion is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        var modelo = _mapper.Map<TransaccionActualizacionViewModel>(transaccion);

        modelo.MontoAnterior = modelo.Monto;

        if (modelo.TipoOperacionId == TipoOperacion.Gasto)
        {
            modelo.MontoAnterior = transaccion.Monto * -1;
        }

        modelo.CuentaAnteriorId = transaccion.CuentaId;
        modelo.Categorias = await ObtenerCategorias(usuarioId, transaccion.TipoOperacionId);
        modelo.Cuentas = await ObtenerCuentas(usuarioId);
        modelo.UrlRetorno = urlRetorno;

        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(TransaccionActualizacionViewModel modelo)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        if (!ModelState.IsValid)
        {
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
            return View(modelo);
        }

        var cuenta = await _repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);
        if (cuenta is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        var categoria = await _repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
        if (categoria is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        var transaccion = _mapper.Map<Transaccion>(modelo);
        if (modelo.TipoOperacionId == TipoOperacion.Gasto)
        {
            transaccion.Monto *= -1;
        }

        await _repositorioTransacciones.Actualizar(transaccion, modelo.MontoAnterior, modelo.CuentaAnteriorId);

        if (string.IsNullOrEmpty(modelo.UrlRetorno))
        {
            return RedirectToAction("Index");
        }
        else
        {
            return LocalRedirect(modelo.UrlRetorno);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Borrar(int id, string urlRetorno = null)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var transaccion = await _repositorioTransacciones.ObtenerPorId(id, usuarioId);
        if (transaccion is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        await _repositorioTransacciones.Borrar(id);
        
        if (string.IsNullOrEmpty(urlRetorno))
        {
            return RedirectToAction("Index");
        }
        else
        {
            return LocalRedirect(urlRetorno);
        }
    }

    private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, TipoOperacion tipoOperacion)
    {
        var categorias = await _repositorioCategorias.Obtener(usuarioId, tipoOperacion);
        return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
    }
}