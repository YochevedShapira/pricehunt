using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PriceHunt.Infrastructure.Tests.Persistence;

public class MigrationsTests
{
    private SqliteTestDatabase _database = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new SqliteTestDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task Migrate_LeavesNoPendingMigrations()
    {
        using var db = _database.CreateContext();

        var pending = await db.Database.GetPendingMigrationsAsync();

        Assert.That(pending, Is.Empty);
    }

    /// <summary>
    /// Fails when the entity configuration changed without a new migration,
    /// so a model change can never ship with a stale schema.
    /// </summary>
    [Test]
    public void ModelSnapshot_MatchesTheCurrentModel()
    {
        using var db = _database.CreateContext();
        var snapshotModel = db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        var currentModel = db.GetService<IDesignTimeModel>().Model;
        var initializedSnapshot = db.GetService<IModelRuntimeInitializer>()
            .Initialize(snapshotModel);

        var hasDifferences = db.GetService<IMigrationsModelDiffer>().HasDifferences(
            initializedSnapshot.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.That(hasDifferences, Is.False);
    }
}
