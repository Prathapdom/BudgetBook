# BudgetBook

A minimal local budget / expense tracker for Windows, built with **.NET 8 + WinUI 3
(Windows App SDK)**. Feature reference: [flow-mn/flow](https://github.com/flow-mn/flow).
This is a learning project to go through Windows app development and Microsoft Store
publishing end to end.

## What it does

- **Dashboard** – month picker, income / expense / net tiles, spending-by-category bars.
- **Transactions** – list by month, add / edit / delete via dialog (type, amount, date,
  account, category, note).
- **Accounts** – CRUD, live balance = opening balance + signed transactions.
- **Categories** – CRUD with color + kind (expense / income / transfer).
- **Settings** – light/dark/system theme, CSV export, open data folder.

Data is a local SQLite file (EF Core) at
`%LOCALAPPDATA%\BudgetBook\budgetbook.db`, created and seeded on first run.
Appearance preferences are stored beside it in `settings.json`.
For isolated development or screenshot runs, set `BUDGETBOOK_DATA_DIR` to a temporary
folder before launching the app.

## Project layout

| Path | Purpose |
|---|---|
| `Data/Models.cs` | `Account`, `Category`, `TransactionRecord`, `TransactionType` |
| `Data/AppDbContext.cs` | EF Core context, migration startup, DB path, starter-data seed |
| `Data/Migrations/` | Tracked EF Core schema migrations |
| `Services/Interfaces/` | Contracts for transaction, account, category, report, and settings operations |
| `Services/*.cs` | Async data access; each operation uses a short-lived context |
| `Reports/` | Pure finance calculations and chart-ready report models |
| `Settings/` | Appearance preference models and live theme application |
| `Themes/` | Semantic color, spacing, typography, and control resources |
| `ViewModels/*.cs` | CommunityToolkit.Mvvm view models, one per page |
| `Converters.cs` | hex→brush, fraction→width, currency, bool→visibility |
| `Pages/*.xaml` | Dashboard / Transactions / Accounts / Categories / Settings |
| `MainWindow.xaml` | `NavigationView` shell |
| `App.xaml.cs` | startup, dependency registration, database initialization, theme root |
| `BudgetBook.Tests/` | Calculation and legacy-database migration tests |
| `Package.appxmanifest` | MSIX identity + visual assets (Partner Center rewrites Identity) |

## Toolchain (this repo is set up without Visual Studio)

- .NET 8 SDK at `F:\dev\dotnet` (`DOTNET_ROOT`, on `PATH`).
- NuGet cache at `F:\dev\nuget\packages` (`NUGET_PACKAGES`).
- WinUI CLI templates: `dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`.

`WindowsAppSDKSelfContained` / `SelfContained` are **true** so the app runs on machines
without the Windows App Runtime installed. For the Store you can flip these to `false`
(framework-dependent) to cut the package from ~110 MB to a few MB — the Store delivers
the WinAppSDK framework package as a dependency.

## Build / run / package

```powershell
$env:DOTNET_ROOT="F:\dev\dotnet"; $env:Path="F:\dev\dotnet;$env:Path"
$env:NUGET_PACKAGES="F:\dev\nuget\packages"
cd F:\Microsoft\Personal

# build
dotnet build -c Debug

# run tests
dotnet test .\BudgetBook.Tests\BudgetBook.Tests.csproj -c Debug

# run (self-contained exe; no VS, no runtime install needed)
& .\bin\Debug\net8.0-windows10.0.26100.0\win-x64\BudgetBook.exe

# make a sideload MSIX
dotnet build -c Release -p:Platform=x64 `
  -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageDir="F:\Microsoft\Personal\AppPackages\" `
  -p:UapAppxPackageBuildMode=SideloadOnly -p:AppxBundle=Never
# -> AppPackages\BudgetBook_1.0.0.0_x64_Test\BudgetBook_1.0.0.0_x64.msix
```

The `mspdbcmf.exe not found` warning during packaging only skips the symbols package;
the MSIX itself is fine. Install the "Windows App SDK C++ / MSIX" bits with Visual
Studio if you want symbol packages.

## Publishing to the Microsoft Store (outline)

1. Register at **partner.microsoft.com** with your Microsoft account, pay the one-time
   developer fee (~US$19 individual). No annual renewal.
2. **Reserve the app name** in Partner Center → note the Identity/Publisher values it
   assigns; put them into `Package.appxmanifest` (or associate via Visual Studio).
3. Build the Store package (`UapAppxPackageBuildMode=StoreUpload`, both `x64` and
   `ARM64`). Run **Windows App Certification Kit** on it — must pass.
4. Fill the submission: description, screenshots (≥1, 1366×768+), **Finance** category,
   IARC age-rating questionnaire, **privacy policy URL** (required even offline),
   pricing (free), markets.
5. Submit → certification (hours to a few days) → published.
6. Updates: bump `Version` in the manifest, rebuild, re-upload.

## Ideas / next steps

- Budgets per category per month + progress on the dashboard.
- Recurring transactions; transfers that touch two accounts.
- CSV import; multi-currency conversion.
- Real chart control (LiveChartsCore) instead of hand-drawn bars.
- EF Core migrations instead of `EnsureCreated` once the schema starts changing.
