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
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeManager, ThemeManager>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TransactionsViewModel>();
        services.AddTransient<AccountsViewModel>();
        services.AddTransient<AccountDetailViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<BudgetsViewModel>();

        return services.BuildServiceProvider();
    }
}
