using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

/// <summary>
/// One in-memory SQLite database per test. The connection is kept open
/// because SQLite drops an in-memory database when its last connection
/// closes. The schema is created by running the real migrations, so the
/// tests also prove the migrations build a usable database.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PriceHuntDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<PriceHuntDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext();
        db.Database.Migrate();
    }

    public PriceHuntDbContext CreateContext()
    {
        return new PriceHuntDbContext(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
