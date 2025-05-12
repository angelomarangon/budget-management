using System.Data;
using BudgetManagement.Models;
using Dapper;
using Npgsql;

namespace BudgetManagement.Services;

public interface IRepositorioUsuarios
{
    Task<int> CrearUsuario(Usuario usuario);
    Task<Usuario> BuscarUsuarioPorEmail(string emailNormalizado);
    Task Actualizar(Usuario usuario);
}

public class RepositorioUsuarios : IRepositorioUsuarios
{
    private readonly string connectionString;
    
    public RepositorioUsuarios(IConfiguration config)
    {
        connectionString = config.GetConnectionString("DefaultConnection");
    }

    public async Task<int> CrearUsuario(Usuario usuario)
    {
        // usuario.EmailNormalizado = usuario.Email.ToUpper(); esto ya se hace en el UsuarioStore
        using var connection = new NpgsqlConnection(connectionString);
        var usuarioId = await connection.QuerySingleAsync<int>(@"
            INSERT INTO Usuarios (Email, EmailNormalizado, PasswordHash)
            VALUES (@Email, @EmailNormalizado, @PasswordHash)
            RETURNING Id", usuario);
        
        await connection.ExecuteAsync(
            "SELECT CrearDatosUsuarioNuevo(@UsuarioId);", 
            new { usuarioId });
        
        return usuarioId;
    }

    public async Task<Usuario> BuscarUsuarioPorEmail(string emailNormalizado)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<Usuario>(@"
            SELECT * FROM Usuarios
            WHERE EmailNormalizado = @emailNormalizado", new { emailNormalizado });
    }

    public async Task Actualizar(Usuario usuario)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(@"
            UPDATE Usuarios
            SET PasswordHash = @PasswordHash
            WHERE Id = @Id", usuario);
    }
}