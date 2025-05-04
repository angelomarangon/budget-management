using BudgetManagement.Models;
using BudgetManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManagement.Controllers;

public class CategoriasController:Controller
{
    private readonly IRepositorioCategorias _repositorioCategorias;
    private readonly IServicioUsuarios _servicioUsuarios;

    public CategoriasController(
        IRepositorioCategorias repositorioCategorias,
        IServicioUsuarios servicioUsuarios)
    {
        _repositorioCategorias = repositorioCategorias;
        _servicioUsuarios = servicioUsuarios;
    }


    public async Task<IActionResult> Index()
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var categorias = await _repositorioCategorias.Obtener(usuarioId);
        
        return View(categorias);
    }
    
    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Categoria categoria)
    {
        if (!ModelState.IsValid)
        {
            return View(categoria);
        }
        
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        categoria.UsuarioId = usuarioId;
        await _repositorioCategorias.Crear(categoria);

        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Editar(int id)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var categoria = await _repositorioCategorias.ObtenerPorId(id, usuarioId);
        if (categoria is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }
        
        return View(categoria);
    }


    [HttpPost]
    public async Task<IActionResult> Editar(Categoria categoriaEditar)
    {
        if (!ModelState.IsValid)
        {
            return View(categoriaEditar);
        }
        
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var categoria = await _repositorioCategorias.ObtenerPorId(categoriaEditar.Id, usuarioId);
        if (categoria is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }
        
        categoriaEditar.UsuarioId = usuarioId;
        await _repositorioCategorias.Actualizar(categoriaEditar);
        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Borrar(int id)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var categoria = await _repositorioCategorias.ObtenerPorId(id, usuarioId);
        if (categoria is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }
        
        return View(categoria);
    }


    [HttpPost]
    public async Task<IActionResult> BorrarCategoria(int id)
    {
        var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
        var categoria = await _repositorioCategorias.ObtenerPorId(id, usuarioId);
        if (categoria is null)
        {
            return RedirectToAction("NoEncontrado", "Home");
        }

        await _repositorioCategorias.Borrar(id);
        return RedirectToAction("Index");
    }

    
}