using BudgetManagement.Models;
using Dapper;
using Npgsql;

namespace BudgetManagement.Services;

public interface IRepositorioCategorias
{
    Task Crear(Categoria categoria);
    Task<IEnumerable<Categoria>> Obtener(int usuarioId, PaginacionViewModel paginacion);
    Task<Categoria> ObtenerPorId(int id, int usuarioId);
    Task Actualizar(Categoria categoria);
    Task Borrar(int id);
    Task<IEnumerable<Categoria>> Obtener(int usuarioId, TipoOperacion tipoOperacionId);
    Task<int> Contar(int usuarioId);
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
            INSERT INTO Categorias (Nombre, TipoOperacionId, UsuarioId)
            VALUES (@Nombre, @TipoOperacionId, @UsuarioId)
            RETURNING Id;", categoria);
        
        categoria.Id = id;
    }

    public async Task<IEnumerable<Categoria>> Obtener(int usuarioId, PaginacionViewModel paginacion)
    {
        using var connection = new NpgsqlConnection(connectionString);
        
        var query = @"
            SELECT * 
            FROM Categorias
            WHERE UsuarioId = @UsuarioId
            ORDER BY Nombre
            OFFSET @Offset ROWS 
            FETCH NEXT @RowsPerPage ROWS ONLY";
        
        return await connection.QueryAsync<Categoria>(query, new
        {
            UsuarioId = usuarioId,
            Offset = paginacion.RecordsASaltar,
            RowsPerPage = paginacion.RecordsPorPagina
        });
        
        // return await connection.QueryAsync<Categoria>(@"
        //     SELECT * 
        //     FROM Categorias
        //     WHERE UsuarioId = @usuarioId
        //     ORDER BY Nombre
        //     OFFSET @paginacion.RecordsASaltar ROWS FETCH NEXT @paginacion.RecordsPorPagina 
        //     ROWS ONLY", new { usuarioId });
    }


    public async Task<int> Contar(int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Categorias
            WHERE UsuarioId = @usuarioId", new { usuarioId });
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
            SET Nombre = @Nombre, TipoOperacionId = @TipoOperacionId
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
            WHERE UsuarioId = @usuarioId AND TipoOperacionId = @tipoOperacionId;", new { usuarioId, tipoOperacionId });
    }
    
}