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
        var subject = "Ha olvidado su contraseña?";
        var contenidoHtml = $@"
        Saludos,
        Este mensaje le llega porque usted ha solicitado un cambio de contraseña. Si esta solicitud no fue hecha por usted, puede ignorar este mensaje.
        Para cambiar su contraseña, haga click en el siguiente enlace
        
        {enlace}

        Atentamente,
        Equipo BudgetManagement
        ";
        
        var mensaje = new MailMessage(emisor, receptor, subject, contenidoHtml);
        await cliente.SendMailAsync(mensaje);
    }
}