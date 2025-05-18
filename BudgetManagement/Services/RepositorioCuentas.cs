using BudgetManagement.Models;
using Dapper;
using Npgsql;

namespace BudgetManagement.Services;

public interface IRepositorioCuentas
{
    Task Crear(Cuenta cuenta);
    Task<IEnumerable<Cuenta>> Buscar(int usuarioId);
    Task<Cuenta> ObtenerPorId(int id, int usuarioId);
    Task Actualizar(CuentaCreacionViewModel cuenta);
    Task Borrar(int id);
}

public class RepositorioCuentas : IRepositorioCuentas
{
    private readonly string connectionString;

    public RepositorioCuentas(IConfiguration configuracion)
    {
        // connectionString = configuracion.GetConnectionString("DefaultConnection");
        connectionString = configuracion["POSTGRESQLCONNSTR_DefaultConnection"];
    }


    public async Task Crear(Cuenta cuenta)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var id = await connection.QuerySingleAsync<int>(@"
            INSERT INTO cuentas (nombre, tipocuentaid, descripcion, balance)
            VALUES (@Nombre, @TipoCuentaId, @Descripcion, @Balance)
            RETURNING Id;", cuenta);
        cuenta.Id = id;
    }

    public async Task<IEnumerable<Cuenta>> Buscar(int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<Cuenta>(@"
            SELECT Cuentas.Id, Cuentas.Nombre, Balance, tc.Nombre as TipoCuenta
            FROM cuentas
            INNER JOIN TiposCuentas tc 
            ON tc.id = Cuentas.tipocuentaid
            WHERE tc.usuarioid = @usuarioId
            ORDER BY tc.orden", new {usuarioId});
    }

    public async Task<Cuenta> ObtenerPorId(int id, int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<Cuenta>(@"
            SELECT Cuentas.Id, Cuentas.Nombre, Balance, descripcion, TipoCuentaId 
            FROM cuentas
            INNER JOIN TiposCuentas tc 
            ON tc.id = Cuentas.tipocuentaid
            WHERE tc.usuarioid = @usuarioId AND cuentas.id = @id", new {id, usuarioId});
    }

    public async Task Actualizar(CuentaCreacionViewModel cuenta)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(@"
            UPDATE cuentas
            SET Nombre = @Nombre, Descripcion = @Descripcion, Balance = @Balance, TipoCuentaId = @TipoCuentaId
            WHERE Id = @Id;", cuenta);
    }


    public async Task Borrar(int id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync("DELETE FROM cuentas WHERE id = @Id;", new { id });
    }
    
}