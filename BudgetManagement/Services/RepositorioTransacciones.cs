using BudgetManagement.Models;
using Dapper;
using Npgsql;

namespace BudgetManagement.Services;

public interface IRepositorioTransacciones
{
    Task Crear(Transaccion transaccion);
    Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnterior);
    Task<Transaccion> ObtenerPorId(int id, int usuarioId);
    Task Borrar(int id);
    Task<IEnumerable<Transaccion>> ObtenerPorCuentaId(ObtenerTransaccionesPorCuenta modelo);
    Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorUsuario modelo);

    Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(
        ParametroObtenerTransaccionesPorUsuario modelo);

    Task<IEnumerable<ResultadoObtenerPorMes>> ObtenerPorMes(
        int usuarioId, int anio);
}

public class RepositorioTransacciones : IRepositorioTransacciones
{
    private readonly string connectionString;

    public RepositorioTransacciones(IConfiguration config)
    {
        // connectionString = config.GetConnectionString("DefaultConnection");
        connectionString = config["POSTGRESQLCONNSTR_DefaultConnection"];
    }


    public async Task Crear(Transaccion transaccion)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Iniciar una transacción
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Insertar la nueva transacción
            var id = await connection.QuerySingleAsync<int>(@"
            INSERT INTO Transacciones (UsuarioId, FechaTransaccion, Monto, CategoriaId, CuentaId, Nota)
            VALUES (@UsuarioId, @FechaTransaccion, @Monto, @CategoriaId, @CuentaId, @Nota)
            RETURNING Id;",
                new
                {
                    transaccion.UsuarioId,
                    transaccion.FechaTransaccion,
                    transaccion.Monto,
                    transaccion.CategoriaId,
                    transaccion.CuentaId,
                    transaccion.Nota
                }, transaction: transaction);

            // Establecer el Id de la transacción
            transaccion.Id = id;

            // Actualizar el balance de la cuenta correspondiente
            await connection.ExecuteAsync(@"
            UPDATE Cuentas
            SET Balance = Balance + @Monto
            WHERE Id = @CuentaId;",
                new { transaccion.Monto, transaccion.CuentaId }, transaction: transaction);

            // Confirmar la transacción
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            // En caso de error, hacer rollback
            await transaction.RollbackAsync();
            throw new Exception("Error al crear la transacción", ex);
        }
    }

    public async Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnteriorId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Iniciar una transacción
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Primero revertir la transacción anterior
            await connection.ExecuteAsync(@"
            UPDATE Cuentas
            SET Balance = Balance - @MontoAnterior
            WHERE Id = @CuentaAnteriorId;",
                new { MontoAnterior = montoAnterior, CuentaAnteriorId = cuentaAnteriorId },
                transaction);

            // Luego realizar la nueva transacción
            await connection.ExecuteAsync(@"
            UPDATE Cuentas
            SET Balance = Balance + @Monto
            WHERE Id = @CuentaId;",
                new { Monto = transaccion.Monto, CuentaId = transaccion.CuentaId },
                transaction);

            // Actualizar la transacción en la tabla Transacciones
            await connection.ExecuteAsync(@"
            UPDATE Transacciones
            SET Monto = ABS(@Monto), 
                FechaTransaccion = @FechaTransaccion, 
                CategoriaId = @CategoriaId, 
                CuentaId = @CuentaId, 
                Nota = @Nota
            WHERE Id = @Id;",
                new
                {
                    Monto = transaccion.Monto,
                    FechaTransaccion = transaccion.FechaTransaccion,
                    CategoriaId = transaccion.CategoriaId,
                    CuentaId = transaccion.CuentaId,
                    Nota = transaccion.Nota,
                    Id = transaccion.Id
                },
                transaction);

            // Confirmar la transacción si todo fue exitoso
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            // Revertir cualquier cambio en caso de error
            await transaction.RollbackAsync();
            throw; // Re-lanzar la excepción
        }
    }

    public async Task<Transaccion> ObtenerPorId(int id, int usuarioId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<Transaccion>(@"
            SELECT Transacciones.*, cat.TipoOperacionId 
            FROM Transacciones
            INNER JOIN Categorias cat
            ON cat.Id = Transacciones.CategoriaId
            WHERE Transacciones.Id = @Id AND Transacciones.UsuarioId = @UsuarioId",
            new { Id = id, UsuarioId = usuarioId });
    }

    public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(
        ParametroObtenerTransaccionesPorUsuario modelo)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<ResultadoObtenerPorSemana>(@"
            SELECT FLOOR(EXTRACT(DAY FROM FechaTransaccion - @fechaInicio) / 7) + 1 AS Semana,
            SUM(Monto) AS Monto, cat.TipoOperacionId
            FROM Transacciones
            INNER JOIN Categorias cat
            ON cat.Id = Transacciones.CategoriaId
            WHERE Transacciones.UsuarioId = @usuarioId AND
            FechaTransaccion BETWEEN @fechaInicio AND @fechaFin
            GROUP BY FLOOR(EXTRACT(DAY FROM FechaTransaccion - @fechaInicio) / 7) + 1, cat.TipoOperacionId", modelo);
    }
    
    public async Task<IEnumerable<ResultadoObtenerPorMes>> ObtenerPorMes(
        int usuarioId, int anio)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<ResultadoObtenerPorMes>(@"
            SELECT EXTRACT(MONTH FROM FechaTransaccion) AS Mes, SUM(Monto) AS Monto, cat.TipoOperacionId
            FROM Transacciones t
            INNER JOIN Categorias cat 
            ON cat.Id = t.CategoriaId
            WHERE t.UsuarioId = @usuarioId AND EXTRACT(YEAR FROM FechaTransaccion) = @anio
            GROUP BY EXTRACT(MONTH FROM FechaTransaccion), cat.TipoOperacionId", new {usuarioId, anio});
    }

    public async Task Borrar(int id)
    {
        using var connection = new NpgsqlConnection(connectionString);
    
        // Llamada a la función almacenada para borrar la transacción
        await connection.ExecuteAsync(
            "SELECT Transacciones_Borrar(@Id);", 
            new { Id = id });
    }


    public async Task<IEnumerable<Transaccion>> ObtenerPorCuentaId(ObtenerTransaccionesPorCuenta modelo)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<Transaccion>(@"
            SELECT t.Id, t.Monto, t.FechaTransaccion, c.Nombre AS Categoria, cu.Nombre AS Cuenta, c.TipoOperacionId
            FROM Transacciones t
            INNER JOIN Categorias c
            ON c.Id = t.CategoriaId
            INNER JOIN Cuentas cu
            ON cu.Id = t.CuentaId
            WHERE t.CuentaId = @CuentaId AND t.UsuarioId = @UsuarioId
            AND FechaTransaccion BETWEEN @FechaInicio AND @FechaFin", modelo);
    }
    
    public async Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorUsuario modelo)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<Transaccion>(@"
            SELECT t.Id, t.Monto, t.FechaTransaccion, c.Nombre AS Categoria, cu.Nombre AS Cuenta, c.TipoOperacionId, Nota
            FROM Transacciones t
            INNER JOIN Categorias c
            ON c.Id = t.CategoriaId
            INNER JOIN Cuentas cu
            ON cu.Id = t.CuentaId
            WHERE t.UsuarioId = @UsuarioId
            AND FechaTransaccion BETWEEN @FechaInicio AND @FechaFin
            ORDER BY t.FechaTransaccion DESC", modelo);
    }

    
}