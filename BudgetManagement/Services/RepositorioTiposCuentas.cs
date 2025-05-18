using BudgetManagement.Models;
using Dapper;
using Npgsql;

namespace BudgetManagement.Services;

public interface IRepositorioTiposCuentas
{
    Task Crear(TipoCuenta tipoCuenta);
    Task<bool> Existe(string nombre, int usuarioId, int id = 0);
    Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId);
    Task Actualizar(TipoCuenta tipoCuenta);
    Task<TipoCuenta> ObtenerPorId(int id, int usuarioId);
    Task Borrar(int id);
    Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasOrdenados);
}

public class RepositorioTiposCuentas : IRepositorioTiposCuentas
{
    private readonly string connectionString;

    public RepositorioTiposCuentas(IConfiguration config)
    {
        // connectionString = config.GetConnectionString("DefaultConnection");
        connectionString = config["POSTGRESQLCONNSTR_DefaultConnection"];
    }

    public async Task Crear(TipoCuenta tipoCuenta)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Obtener el siguiente valor de Orden (maximo + 1)
        var maxOrden = await connection.QuerySingleOrDefaultAsync<int>(@"
        SELECT COALESCE(MAX(Orden), 0) + 1
        FROM TiposCuentas
        WHERE UsuarioId = @UsuarioId;", 
            new { tipoCuenta.UsuarioId });

        // Insertar el nuevo registro con el Orden calculado
        var id = await connection.QuerySingleAsync<int>(@"
        INSERT INTO TiposCuentas (Nombre, UsuarioId, Orden)
        VALUES (@Nombre, @UsuarioId, @Orden)
        RETURNING Id;", 
            new { tipoCuenta.Nombre, tipoCuenta.UsuarioId, Orden = maxOrden });

        tipoCuenta.Id = id;
    }

    public async Task<bool> Existe(string nombre, int usuarioId, int id = 0)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var existe = await connection.QueryFirstOrDefaultAsync<int>(@"
            SELECT 1 
            FROM TiposCuentas
            WHERE Nombre = @nombre AND UsuarioId = @usuarioId AND Id <> @id;", new { nombre, usuarioId, id }
        );
        return existe == 1;
    }

    public async Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<TipoCuenta>(@"
            SELECT Id, Nombre, Orden
            FROM TiposCuentas   
            WHERE UsuarioId = @UsuarioId
            ORDER BY Orden", new { usuarioId });
    }

    public async Task Actualizar(TipoCuenta tipoCuenta)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TiposCuentas
            SET Nombre = @Nombre
            WHERE Id = @Id", tipoCuenta);
    }

    public async Task<TipoCuenta> ObtenerPorId(int id, int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<TipoCuenta>(@"
            SELECT Id, Nombre, Orden
            FROM TiposCuentas
            WHERE Id = @Id AND UsuarioId = @UsuarioId;",
            new { id, usuarioId });
    }

    public async Task Borrar(int id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(@"
            DELETE FROM TiposCuentas WHERE Id = @Id", new { id });
    }



    public async Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasOrdenados)
    {
        var query = "UPDATE TiposCuentas SET Orden = @Orden WHERE Id = @Id;";
        using var connection = new NpgsqlConnection(connectionString);
        
        await connection.ExecuteAsync(query, tipoCuentasOrdenados);
    }
}