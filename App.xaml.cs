using Microsoft.UI.Xaml;
using BudgetBook.Data;
using BudgetBook.Services;
using BudgetBook.Services.Interfaces;
using BudgetBook.Settings;
using BudgetBook.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using BudgetBook.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BudgetBook;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>Root element of the main window, used for runtime theme switching.</summary>
    public static FrameworkElement? RootElement { get; set; }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // SkiaSharp's WinUI GPU surface can fail on some Windows/driver combinations.
        // Charts are small and static enough that the more reliable CPU renderer is preferable.
        LiveCharts.Configure(configuration => configuration
            .HasRenderingSettings(settings => settings.UseGPU = false));
        InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        AppDbContext.Initialize(Services.GetRequiredService<IDbContextFactory<AppDbContext>>());

        _window = new MainWindow();
        if (RootElement is { } root)
        {
            GetService<IThemeManager>().Apply(root, GetService<ISettingsService>().Preferences);
        }
        _window.Activate();
        TryHandleProtocolActivation(AppInstance.GetCurrent().GetActivatedEventArgs());
    }

    private static void TryHandleProtocolActivation(AppActivationArguments arguments)
    {
        if (arguments.Kind != ExtendedActivationKind.Protocol || arguments.Data is not ProtocolActivatedEventArgs protocol) return;
        var uri = protocol.Uri;
        if (!uri.Host.Equals("add", StringComparison.OrdinalIgnoreCase) && !uri.AbsolutePath.Trim('/').Equals("add", StringComparison.OrdinalIgnoreCase)) return;
        var values = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2)).ToDictionary(part => Uri.UnescapeDataString(part[0]), part => part.Length > 1 ? Uri.UnescapeDataString(part[1]) : "", StringComparer.OrdinalIgnoreCase);
        _ = decimal.TryParse(values.GetValueOrDefault("amount"), out var amount);
        _ = int.TryParse(values.GetValueOrDefault("accountId"), out var accountId);
        _ = int.TryParse(values.GetValueOrDefault("categoryId"), out var categoryId);
        _ = Enum.TryParse<TransactionType>(values.GetValueOrDefault("type"), true, out var type);
        _ = DateTime.TryParse(values.GetValueOrDefault("date"), out var date);
        var draft = new TransactionRecord { Amount = Math.Abs(amount), AccountId = accountId, CategoryId = categoryId == 0 ? null : categoryId,
            Type = type, Date = date == default ? DateTime.Today : date.Date, Note = values.GetValueOrDefault("note") ?? "", Tags = values.GetValueOrDefault("tags") ?? "" };
        MainWindow.Instance?.NavigateToTransactions(new TransactionsNavigationRequest(draft.Date, OpenNewTransaction: true, Draft: draft));
    }

    public static T GetService<T>() where T : notnull => Services.GetRequiredService<T>();

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={AppDbContext.DbPath}"));

        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IBudgetService, BudgetService>();
        services.AddSingleton<IRecurringTransactionService, RecurringTransactionService>();
        services.AddSingleton<IAttachmentService, AttachmentService>();
        services.AddSingleton<IExchangeRateService, ExchangeRateService>();
        services.AddSingleton<ICsvImportService, CsvImportService>();
        services.AddSingleton<IPdfReportService, PdfReportService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeManager, ThemeManager>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TransactionsViewModel>();
        services.AddTransient<AccountsViewModel>();
        services.AddTransient<AccountDetailViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<BudgetsViewModel>();
        services.AddTransient<RecurringViewModel>();

        return services.BuildServiceProvider();
    }
}
