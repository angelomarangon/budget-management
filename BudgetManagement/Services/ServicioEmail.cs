using System.Net;
using System.Net.Mail;

namespace BudgetManagement.Services;

public interface IServicioEmail
{
    Task EnviarEmailCambioPassword(string receptor, string enlace);
}

public class ServicioEmail : IServicioEmail
{
    private readonly IConfiguration _config;


    public ServicioEmail(IConfiguration config)
    {
        _config = config;
    }

    public async Task EnviarEmailCambioPassword(string receptor, string enlace)
    {
        var email = _config["CONFIGURACIONES_EMAIL:EMAIL"];
        var password = _config["CONFIGURACIONES_EMAIL:PASSWORD"];
        var host = _config["CONFIGURACIONES_EMAIL:HOST"];
        var puerto = int.Parse(_config["CONFIGURACIONES_EMAIL:PUERTO"]!);

        // var email = _config.GetValue<string>("CONFIGURACIONES_EMAIL:EMAIL");
        // var password = _config.GetValue<string>("CONFIGURACIONES_EMAIL:PASSWORD");
        // var host = _config.GetValue<string>("CONFIGURACIONES_EMAIL:HOST");
        // var puerto = _config.GetValue<int>("CONFIGURACIONES_EMAIL:PUERTO");

        var cliente = new SmtpClient(host, puerto);
        cliente.EnableSsl = true;
        cliente.UseDefaultCredentials = false;

        cliente.Credentials = new NetworkCredential(email, password);
        var emisor = email;
        var subject = "Recuperación de contraseña - BudgetManagement";
        var contenidoHtml = $@"
            <p>Hola,</p>

            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <strong>BudgetManagement</strong>.</p>

            <p>Si realizaste esta solicitud, haz clic en el siguiente botón para crear una nueva contraseña:</p>

            <div style='margin: 2em 0; text-align: left;'>
                <a href='{enlace}'
                   style='background-color: #8b5cf6; padding: 14px 28px; 
                          border-radius: 10px; display: inline-block; text-decoration: none;'>
                   <span style='color: #ffffff !important; font-weight: bold; font-size: 16px;'>
                       Restablecer contraseña
                   </span>
                </a>
            </div>

            <p>Si no realizaste esta solicitud, puedes ignorar este mensaje. Tu cuenta permanecerá segura.</p>

            <p style='margin-top: 2rem;'>Atentamente,<br />
            El equipo de <strong>BudgetManagement</strong></p>";

        var mensaje = new MailMessage(emisor!, receptor, subject, contenidoHtml)
        {
            IsBodyHtml = true // <- esta línea es crucial
        };
        
        await cliente.SendMailAsync(mensaje);
    }
}