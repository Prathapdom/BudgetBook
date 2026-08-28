using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Data;

public class AppDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    public static string DbFolder
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BudgetBook");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DbPath => Path.Combine(DbFolder, "budgetbook.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

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

        // SQLite has no decimal type; store as TEXT with invariant culture ordering.
        foreach (var p in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            p.SetColumnType("TEXT");
        }
    }

    /// <summary>Create the database on first run and seed starter data.</summary>
    public static void Initialize()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();

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
}
