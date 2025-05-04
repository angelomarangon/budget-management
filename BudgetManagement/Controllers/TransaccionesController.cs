using AutoMapper;
using BudgetManagement.Models;
using BudgetManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManagement.Controllers;

public class TransaccionesController:Controller
{
    private readonly IServicioUsuarios _servicioUsuarios;
    private readonly IRepositorioTransacciones _repositorioTransacciones;
    private readonly IRepositorioCuentas _repositorioCuentas;
    private readonly IRepositorioCategorias _repositorioCategorias;
    private readonly IMapper _mapper;

    public TransaccionesController(
        IServicioUsuarios servicioUsuarios,
        IRepositorioTransacciones repositorioTransacciones,
        IRepositorioCuentas repositorioCuentas,
        IRepositorioCategorias repositorioCategorias, 
        IMapper mapper)
    {
        _servicioUsuarios = servicioUsuarios;
        _repositorioTransacciones = repositorioTransacciones;
        _repositorioCuentas = repositorioCuentas;
        _repositorioCategorias = repositorioCategorias;
        _mapper = mapper;
    }

    public IActionResult Index()
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
    public async Task<IActionResult> Editar(int id)
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
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Borrar(int id)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var transaccion = await _repositorioTransacciones.ObtenerPorId(id, usuarioId);
        if (transaccion is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }
        await _repositorioTransacciones.Borrar(id);
        return RedirectToAction("Index");
    }

    private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, TipoOperacion tipoOperacion)
    {
        var categorias = await _repositorioCategorias.Obtener(usuarioId, tipoOperacion);
        return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
    }
    
    
    
}