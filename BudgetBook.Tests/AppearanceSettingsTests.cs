using BudgetBook.Data;
using BudgetBook.Services;
using BudgetBook.Services.Interfaces;
using BudgetBook.Settings;

namespace BudgetBook.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AppearanceSettingsTests
{
    private string? _dataDirectory;
    private string? _previousDataDirectory;

    [TestInitialize]
    public void Initialize()
    {
        _previousDataDirectory = Environment.GetEnvironmentVariable("BUDGETBOOK_DATA_DIR");
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"budgetbook-settings-test-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("BUDGETBOOK_DATA_DIR", _dataDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("BUDGETBOOK_DATA_DIR", _previousDataDirectory);
        if (_dataDirectory is not null && Directory.Exists(_dataDirectory))
            Directory.Delete(_dataDirectory, recursive: true);
    }

    [TestMethod]
    public void SaveAppearance_IsLoadedByNextServiceInstance()
    {
        var writer = new SettingsService(new StubTransactionService());
        writer.SaveAppearance(AppThemeMode.Dark, AppAccentPalette.Violet, AppDensity.Compact);

        var reader = new SettingsService(new StubTransactionService());

        Assert.AreEqual(AppThemeMode.Dark, reader.Preferences.ThemeMode);
        Assert.AreEqual(AppAccentPalette.Violet, reader.Preferences.AccentPalette);
        Assert.AreEqual(AppDensity.Compact, reader.Preferences.Density);
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public Task<IReadOnlyList<TransactionRecord>> GetTransactionsAsync(
            DateTime? month = null,
            int? accountId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TransactionRecord>>([]);

        public Task SaveTransactionAsync(TransactionRecord transaction, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteTransactionAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
