using BudgetManagement.Models;
using Dapper;
using Npgsql;

namespace BudgetManagement.Services;

public interface IRepositorioCategorias
{
    Task Crear(Categoria categoria);
    Task<IEnumerable<Categoria>> Obtener(int usuarioId);
    Task<Categoria> ObtenerPorId(int id, int usuarioId);
    Task Actualizar(Categoria categoria);
    Task Borrar(int id);
    Task<IEnumerable<Categoria>> Obtener(int usuarioId, TipoOperacion tipoOperacionId);
}

public class RepositorioCategorias : IRepositorioCategorias
{
    private readonly string connectionString;
    
    public RepositorioCategorias(IConfiguration configuracion)
    {
        connectionString = configuracion.GetConnectionString("DefaultConnection");
    }

    public async Task Crear(Categoria categoria)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var id = await connection.QuerySingleAsync<int>(@"
            INSERT INTO Categorias (Nombre, OperacionId, UsuarioId)
            VALUES (@Nombre, @TipoOperacionId, @UsuarioId)
            RETURNING Id;", categoria);
        
        categoria.Id = id;
    }

    public async Task<IEnumerable<Categoria>> Obtener(int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<Categoria>(@"
            SELECT * 
            FROM Categorias
            WHERE UsuarioId = @usuarioId;", new { usuarioId });
    }


    public async Task<Categoria> ObtenerPorId(int id, int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<Categoria>(@"
            SELECT *  
            FROM Categorias
            WHERE Id = @id AND UsuarioId = @UsuarioId", new { id, usuarioId });
    }

    public async Task Actualizar(Categoria categoria)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(@"
            UPDATE Categorias
            SET Nombre = @Nombre, OperacionId = @TipoOperacionId
                WHERE Id = @Id", categoria);
    }


    public async Task Borrar(int id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(@"
            DELETE FROM Categorias
            WHERE Id = @Id;", new { id });
    }

    public async Task<IEnumerable<Categoria>> Obtener(int usuarioId, TipoOperacion tipoOperacionId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<Categoria>(@"
            SELECT * 
            FROM Categorias
            WHERE UsuarioId = @usuarioId AND OperacionId = @tipoOperacionId;", new { usuarioId, tipoOperacionId });
    }
    
}