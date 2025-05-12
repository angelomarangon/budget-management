using System.Data;
using System.Runtime.InteropServices.JavaScript;
using AutoMapper;
using BudgetManagement.Models;
using BudgetManagement.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
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
    
    public async Task<IActionResult> Mensual(int anio)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        if (anio == 0)
        {
            anio = DateTime.Today.Year;
        }
        
        var transaccionesPorMes = await _repositorioTransacciones.ObtenerPorMes(usuarioId, anio);

        var transaccionesAgrupadas = transaccionesPorMes.GroupBy(x => x.Mes)
            .Select(x => new ResultadoObtenerPorMes()
            {
                Mes = x.Key,
                Ingresos = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso)
                    .Select(x => x.Monto).FirstOrDefault(),
                Gastos = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto)
                    .Select(x => x.Monto).FirstOrDefault(),
            }).ToList();

        for (int mes = 1; mes <= 12; mes++)
        {
            var transaccion = transaccionesAgrupadas.FirstOrDefault(x => x.Mes == mes);
            var fechaReferencia = new DateTime(anio, mes, 1);

            if (transaccion is null)
            {
                transaccionesAgrupadas.Add(new ResultadoObtenerPorMes()
                {
                    Mes = mes,
                    FechaReferencia = fechaReferencia,
                });
            }
            else
            {
                transaccion.FechaReferencia = fechaReferencia;
            }
        }
        
        transaccionesAgrupadas = transaccionesAgrupadas.OrderByDescending(x => x.Mes).ToList();

        var modelo = new ReporteMensualViewModel();
        modelo.Anio = anio;
        modelo.TransaccionesPorMes = transaccionesAgrupadas;
        
        return View(modelo);
    }
    
    public IActionResult ExcelReporte()
    {
        
        return View();
    }

    [HttpGet]
    public async Task<FileResult> ExportarExcelPorMes(int mes, int anio)
    {
        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        var transacciones = await _repositorioTransacciones
            .ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                Fechafin = fechaFin
            });

        var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("MMM yyyy")}.xlsx";

        return GenerarExcel(nombreArchivo, transacciones);
    }

    [HttpGet]
    public async Task<FileResult> ExportarExcelPorAnio(int anio)
    {
        var fechaInicio = new DateTime(anio, anio, 1);
        var fechaFin = fechaInicio.AddYears(1).AddDays(-1);
        
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        var transacciones = await _repositorioTransacciones
            .ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                Fechafin = fechaFin
            });
        
        var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("yyyy")}.xlsx";
        return GenerarExcel(nombreArchivo, transacciones);
    }

    [HttpGet]
    public async Task<FileResult> ExportarExcelTodo()
    {
        var fechaInicio = DateTime.Today.AddYears(-100);
        var fechaFin = DateTime.Today.AddYears(1000);
        
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        var transacciones = await _repositorioTransacciones
            .ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                Fechafin = fechaFin
            });
        
        var nombreArchivo = $"Manejo Presupuestos - {DateTime.Today.ToString("dd-MM-yyyy")}.xlsx";
        return GenerarExcel(nombreArchivo, transacciones);
    }


    private FileResult GenerarExcel(string nombreArchivo, IEnumerable<Transaccion> transacciones)
    {
        DataTable dataTable = new DataTable("Transacciones");
        dataTable.Columns.AddRange(new DataColumn[]
        {
            new DataColumn("Fecha"),
            new DataColumn("Cuenta"),
            new DataColumn("Categoria"),
            new DataColumn("Nota"),
            new DataColumn("Monto"),
            new DataColumn("Ingreso/Gasto"),
        });

        foreach (var transaccion in transacciones)
        {
            dataTable.Rows.Add(
                transaccion.FechaTransaccion,
                transaccion.Cuenta,
                transaccion.Categoria,
                transaccion.Nota,
                transaccion.Monto,
                transaccion.TipoOperacionId
                );
        }

        using (XLWorkbook wb = new XLWorkbook())
        {
            wb.Worksheets.Add(dataTable);

            using (MemoryStream stream = new MemoryStream())
            {
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
        }
    }
    
    public IActionResult Calendario()
    {
        
        return View();
    }

    public async Task<JsonResult> ObtenerTransaccionesCalendario(DateTime start, DateTime end)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        var transacciones = await _repositorioTransacciones
            .ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario
            {
                UsuarioId = usuarioId,
                FechaInicio = start,
                Fechafin = end
            });

        var eventosCalendario = transacciones.Select(transaccion => new EventoCalendario()
        {
            Title = transaccion.Monto.ToString(),
            Start = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
            End = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
            Color = (transaccion.TipoOperacionId == TipoOperacion.Gasto) ? "Red" : null,
        });

        return Json(eventosCalendario);
    }

    public async Task<JsonResult> ObtenerTransaccionesPorFecha(DateTime fecha)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        
        var transacciones = await _repositorioTransacciones
            .ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fecha,
                Fechafin = fecha
            });
        
        return Json(transacciones);
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
        var resultado = categorias
            .Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();

        var opcionPorDefecto = new SelectListItem("-- Selecciones una categoria --", "0", true);
        
        resultado.Insert(0, opcionPorDefecto);
        
        return resultado;
    }
}