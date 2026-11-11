using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CynAstro.Infrastructure.Data;

/// <summary>
/// Provides SQL Server connections for Dapper.
/// Uses connection pooling (enabled by default in SqlClient).
/// Connection string read from configuration — never hardcoded.
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("CynAstroDB")
            ?? throw new InvalidOperationException("Database connection string ('DefaultConnection' or 'CynAstroDB') is not configured.");
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        return connection;
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
