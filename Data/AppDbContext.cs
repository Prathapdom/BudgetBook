using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Data;

public class AppDbContext : DbContext
{
    public const string InitialMigrationId = "20260828170030_InitialCreate";
    public const string BudgetsMigrationId = "20260829143000_AddBudgets";

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
    public DbSet<Budget> Budgets => Set<Budget>();

    public static string DbFolder
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("BUDGETBOOK_DATA_DIR");
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BudgetBook");
            }

            dir = Path.GetFullPath(dir);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DbPath => Path.Combine(DbFolder, "budgetbook.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
            options.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<TransactionRecord>()
            .HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TransactionRecord>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<Budget>()
            .HasOne(budget => budget.Category)
            .WithMany(category => category.Budgets)
            .HasForeignKey(budget => budget.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Budget>()
            .HasIndex(budget => budget.CategoryId)
            .IsUnique();

        // SQLite has no decimal type; store as TEXT with invariant culture ordering.
        foreach (var p in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            p.SetColumnType("TEXT");
        }
    }

    /// <summary>
    /// Migrates the database and seeds starter data. Databases created by older builds with
    /// EnsureCreated are first registered as the initial migration so existing data is kept.
    /// </summary>
    public static void Initialize(IDbContextFactory<AppDbContext> contextFactory)
    {
        using var db = contextFactory.CreateDbContext();
        RegisterLegacyDatabaseIfNeeded(db);
        db.Database.Migrate();

        if (!db.Accounts.Any())
        {
            db.Accounts.Add(new Account { Name = "Cash", OpeningBalance = 0m });
            db.Accounts.Add(new Account { Name = "Checking", OpeningBalance = 0m });
        }

        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Groceries", Glyph = "", ColorHex = "#2E7D32" },
                new Category { Name = "Rent", Glyph = "", ColorHex = "#5E35B1" },
                new Category { Name = "Transport", Glyph = "", ColorHex = "#1565C0" },
                new Category { Name = "Eating out", Glyph = "", ColorHex = "#EF6C00" },
                new Category { Name = "Utilities", Glyph = "", ColorHex = "#00838F" },
                new Category { Name = "Fun", Glyph = "", ColorHex = "#C2185B" },
                new Category { Name = "Salary", Glyph = "", ColorHex = "#2E7D32", Kind = TransactionType.Income });
        }

        db.SaveChanges();
    }

    private static void RegisterLegacyDatabaseIfNeeded(AppDbContext db)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) connection.Open();

        try
        {
            using var tables = connection.CreateCommand();
            tables.CommandText = """
                SELECT
                    EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Accounts'),
                    EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Categories'),
                    EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Transactions'),
                    EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Budgets'),
                    EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory');
                """;

            using var reader = tables.ExecuteReader();
            if (!reader.Read()) return;

            var hasLegacyTables = reader.GetInt64(0) == 1
                && reader.GetInt64(1) == 1
                && reader.GetInt64(2) == 1;
            var hasBudgetsTable = reader.GetInt64(3) == 1;
            var hasMigrationHistory = reader.GetInt64(4) == 1;
            reader.Close();

            if (!hasLegacyTables || hasMigrationHistory) return;

            using var transaction = connection.BeginTransaction();
            using var baseline = connection.CreateCommand();
            baseline.Transaction = transaction;
            baseline.CommandText = """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ($migrationId, '8.0.11');
                """;
            baseline.Parameters.AddWithValue("$migrationId", InitialMigrationId);
            baseline.ExecuteNonQuery();
            if (hasBudgetsTable)
            {
                baseline.CommandText = """
                    INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ($budgetsMigrationId, '8.0.11');
                    """;
                baseline.Parameters.Clear();
                baseline.Parameters.AddWithValue("$budgetsMigrationId", BudgetsMigrationId);
                baseline.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        finally
        {
            if (shouldClose) connection.Close();
        }
    }
}
