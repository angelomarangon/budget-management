using System.Security.Claims;
using System.Text;
using BudgetManagement.Models;
using BudgetManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BudgetManagement.Controllers;

public class UsuariosController : Controller
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;
    private readonly IServicioEmail _servicioEmail;

    public UsuariosController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        IServicioEmail servicioEmail)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _servicioEmail = servicioEmail;
    }

    [AllowAnonymous]
    public IActionResult Registro()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = new Usuario() { Email = modelo.Email };

        var resultado = await _userManager.CreateAsync(usuario, modelo.Password);

        if (resultado.Succeeded)
        {
            await _signInManager.SignInAsync(usuario, isPersistent: true);
            return RedirectToAction("Index", "Transacciones");
        }
        else
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction("Index", "Transacciones");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult OlvideMiPassword(string mensaje = "")
    {
        ViewBag.Mensaje = mensaje;

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> OlvideMiPassword(OlvideMiPasswordViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var mensaje =
            "Hemos enviado un enlace a tu correo electrónico.<br/>Si la dirección proporcionada corresponde a una cuenta registrada, recibirás instrucciones para restablecer tu contraseña.";

        ViewBag.Mensaje = mensaje;
        ModelState.Clear();

        var usuario = await _userManager.FindByEmailAsync(modelo.Email);
        if (usuario is null)
        {
            return View();
        }

        var codigo = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var codigoBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(codigo));
        var enlace = Url.Action("RecuperarPassword", "Usuarios", new { codigo = codigoBase64 },
            protocol: Request.Scheme);

        await _servicioEmail.EnviarEmailCambioPassword(modelo.Email, enlace);
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _signInManager.PasswordSignInAsync(modelo.Email, modelo.Password, modelo.Recuerdame,
            lockoutOnFailure: false);

        if (resultado.Succeeded)
        {
            return RedirectToAction("Index", "Transacciones");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Nombre de usuario o password invalido");
            return View(modelo);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            // solamente si el usuario esta autenticado
            var claims = User.Claims.ToList();
            var usuarioIdReal = claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault();
            var id = usuarioIdReal!.Value;
        }

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RecuperarPassword(string codigo = null)
    {
        if (codigo is null)
        {
            var mensaje = "Codigo no encontrado";
            return RedirectToAction("OlvideMiPassword", new { mensaje });
        }

        var modelo = new RecuperarPasswordViewModel();
        modelo.CodigoReseteo = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(codigo));

        return View(modelo);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarPassword(RecuperarPasswordViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }
        
        var usuario = await _userManager.FindByEmailAsync(modelo.Email);
        if (usuario is null)
        {
            return RedirectToAction("PasswordCambiado");
        }

        var resultados = await _userManager.ResetPasswordAsync(usuario, modelo.CodigoReseteo, modelo.Password);

        return RedirectToAction("PasswordCambiado");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult PasswordCambiado()
    {
        return View();
    }
}