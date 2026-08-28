using BudgetBook.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Tests;

[TestClass]
public sealed class DatabaseMigrationTests
{
    private string? _databasePath;

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        if (_databasePath is not null && File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [TestMethod]
    public void Initialize_RegistersLegacySchemaAndPreservesExistingData()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"budgetbook-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        var factory = new TestContextFactory(options);

        using (var legacy = factory.CreateDbContext())
        {
            legacy.Database.EnsureCreated();
            legacy.Accounts.Add(new Account { Name = "Existing account", OpeningBalance = 123.45m });
            legacy.SaveChanges();
        }

        AppDbContext.Initialize(factory);

        using var migrated = factory.CreateDbContext();
        Assert.IsTrue(migrated.Accounts.Any(a => a.Name == "Existing account" && a.OpeningBalance == 123.45m));

        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = $migrationId";
        command.Parameters.AddWithValue("$migrationId", AppDbContext.InitialMigrationId);
        Assert.AreEqual(1L, (long)command.ExecuteScalar()!);
    }

    [TestMethod]
    public void Initialize_CreatesAndSeedsFreshDatabaseThroughMigrations()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"budgetbook-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        var factory = new TestContextFactory(options);

        AppDbContext.Initialize(factory);

        using var created = factory.CreateDbContext();
        Assert.IsTrue(created.Accounts.Any());
        Assert.IsTrue(created.Categories.Any());
        Assert.AreEqual(AppDbContext.InitialMigrationId, created.Database.GetAppliedMigrations().Single());
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
