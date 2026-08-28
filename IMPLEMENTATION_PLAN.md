# BudgetBook implementation plan

## Recommendation

Keep the existing **.NET 8 + WinUI 3** application and evolve it incrementally. The CRUD and SQLite foundation already works; rewriting it in Flutter merely because Flow uses Flutter would add risk without improving the Windows experience.

Flow’s useful architectural idea is its separation between [reports](https://github.com/flow-mn/flow/tree/develop/lib/reports), [reusable widgets](https://github.com/flow-mn/flow/tree/develop/lib/widgets), routes, preferences, and theme infrastructure. Its public feature set also extends beyond visuals into recurring transactions, backups, import/export, attachments, tags, and multi-currency support. [Flow repository](https://github.com/flow-mn/flow), [Flow website](https://flow.gege.mn/).

One legal consideration: Flow is GPL-3.0. Use it for product and UX inspiration, but do not copy its source or assets unless you intend to release BudgetBook under GPL-3.0 as well.

## Current-state assessment

Your project already has:

- Transactions, accounts, and categories CRUD.
- Monthly income, expenses, net, and category spending.
- SQLite through EF Core.
- Basic light/dark/system switching.
- CSV export.
- A sensible MVVM starting point.

The main gaps are:

| Area | Current implementation | Needed |
|---|---|---|
| Charts | Fixed-width hand-drawn category bars | Donut, trend, and income-vs-expense charts |
| Cards | Repeated page-local `Border` markup | Reusable, responsive card components |
| Themes | System brushes plus runtime light/dark switch | Design tokens, accent palettes, persistence, high contrast |
| Dashboard | Three fixed columns | Responsive finance overview and account cards |
| Reports | Queries mixed into `BudgetService` | Date-range reporting layer and chart-ready DTOs |
| Database | `EnsureCreated()` | EF migrations before adding budgets and recurring data |
| Responsiveness | Mostly fixed widths | Narrow, medium, and wide layouts |
| Performance | Synchronous DB access on UI thread | Async EF queries and cancellable page loading |

Relevant starting files are `App.xaml`, `Pages/DashboardPage.xaml`, `ViewModels/DashboardViewModel.cs`, `Services/BudgetService.cs`, and `Data/Models.cs`.

## Step-by-step implementation plan

### Phase 1 — Protect and restructure the foundation

1. Create a feature branch and capture screenshots of every current page.
2. Add a small test project for report calculations and account balances.
3. Replace `EnsureCreated()` with EF Core migrations before changing the schema.
4. Introduce these folders:

```text
Controls/
    Cards/
    Charts/
    Common/
Reports/
Themes/
    Colors.xaml
    Typography.xaml
    Controls.xaml
    ThemeResources.xaml
Services/
    Interfaces/
Settings/
```

5. Split `BudgetService` responsibilities:

```text
ITransactionService
IAccountService
ICategoryService
IReportService
ISettingsService
```

6. Convert database methods to `Task`-based EF Core calls so dashboard loading does not block the UI.
7. Add application-level dependency registration instead of continuing to grow the static `App.Budget` locator.

Definition of done: all existing functionality behaves identically, migrations work on an existing database, and calculation tests pass.

### Phase 2 — Build the theme and design-token system

Create reusable resources rather than assigning spacing, radius, and colors independently on each page.

1. Add semantic tokens:

```text
PagePadding
SectionSpacing
CardPadding
CardCornerRadius
CardBorderBrush
CardBackgroundBrush
CardBackgroundHoverBrush
PositiveAmountBrush
NegativeAmountBrush
NeutralAmountBrush
ChartGridBrush
ChartPalette1 ... ChartPalette8
```

2. Put light, dark, and high-contrast values into `ThemeDictionaries`.
3. Reference these with `{ThemeResource ...}` throughout the UI so runtime changes propagate correctly.
4. Add three user preferences:

- System/light/dark mode.
- Accent palette.
- Compact/comfortable density.

5. Persist preferences with local app settings and apply them before the main window becomes visible.
6. Preserve the Windows system accent as the default palette.

Microsoft recommends explicit Light, Dark, and HighContrast dictionaries for custom theme resources and dynamic `{ThemeResource}` references. [WinUI theme-resource guidance](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-theme-resources).

Definition of done: every page renders correctly in light, dark, system, and Windows high-contrast modes after restarting the application.

### Phase 3 — Implement reusable cards

Build a small component library before redesigning individual pages.

1. `FlowCard` or `AppCard`

- Standard background, border, radius, padding, and hover state.
- Optional command/click behavior.
- Header, content, and footer slots.

2. `MetricCard`

- Title, primary amount, icon, trend indicator, comparison text.
- Variants for income, expense, balance, and savings.

3. `AccountCard`

- Account name, balance, currency, icon/color.
- Optional percentage of total balance and recent activity.

4. `BudgetProgressCard`

- Category, spent amount, limit, remaining amount, progress.
- Normal, warning, and exceeded states.

5. Supporting controls:

- `SectionHeader`
- `EmptyState`
- `ChartLegend`
- `AmountText`
- `TransactionListItem`
- Loading skeletons for the dashboard

Use `ItemsRepeater` with `UniformGridLayout` for wrapping cards instead of hard-coded three-column grids. This provides responsive wrapping and UI virtualization. [Microsoft ItemsRepeater guidance](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/items-repeater).

Definition of done: dashboard, accounts, and categories share the same card styles without duplicating `Border` definitions.

### Phase 4 — Create the report layer

Charts should consume report models, not raw database entities.

Add report types such as:

```csharp
DashboardSummary
CategoryBreakdown
CashFlowPoint
IncomeExpensePoint
AccountBalanceSummary
PeriodComparison
```

Implement these `IReportService` queries:

1. Summary for a date range.
2. Expense breakdown by category.
3. Daily/weekly/monthly cash-flow series.
4. Income versus expense series.
5. Previous-period comparison.
6. Account balance distribution.
7. Recent transactions.
8. Optional category and account filters.

Centralize the selected period in a `ReportFilter`:

```text
This month
Last month
Last 3 months
Last 6 months
This year
Custom range
```

Perform aggregation in SQLite where practical rather than loading all transactions and grouping them in memory.

Definition of done: report calculations are unit-tested for empty data, income-only, expense-only, transfers, category deletion, and month/year boundaries.

### Phase 5 — Add real charts

Use the stable `LiveChartsCore.SkiaSharpView.WinUI` package. The current stable package is compatible with .NET 8/WinUI and supports interactive WinUI charts. [LiveCharts2 project](https://github.com/Live-Charts/LiveCharts2), [WinUI NuGet package](https://www.nuget.org/packages/LiveChartsCore.SkiaSharpView.WinUI).

Add these charts in order:

1. **Category donut chart**

- Amount and percentage by category.
- Matching legend and category colors.
- Select a slice to filter transactions.

2. **Cash-flow line or area chart**

- Daily net movement during a month.
- Weekly/monthly aggregation for longer ranges.

3. **Income-versus-expense columns**

- Side-by-side monthly comparison.
- Tooltip containing exact amounts.

4. **Account distribution chart**

- Optional donut showing total money by account.

Chart requirements:

- Derive colors from the theme palette.
- Rebuild paint resources when the theme changes.
- Provide tooltips and legends.
- Show a useful empty state instead of an empty plotting surface.
- Avoid relying on color alone; use labels and patterns where possible.
- Disable or shorten animations when reduced-motion settings are enabled.
- Keep tables or summaries available for screen-reader users.

Definition of done: all charts update when the period, account filter, data, or theme changes.

### Phase 6 — Redesign the dashboard

Recommended wide layout:

```text
Period selector                      Quick Add Transaction

Total balance | Income | Expenses | Savings rate

Cash-flow trend (2/3 width)          Category donut (1/3 width)

Accounts
[Account card] [Account card] [Account card]

Budgets                              Recent transactions
```

Implementation steps:

1. Wrap the page in a `ScrollViewer`.
2. Replace the fixed three-column summary grid with responsive metric cards.
3. Add `VisualStateManager` states:

- Narrow: below approximately 720 px, one column.
- Medium: two columns.
- Wide: dashboard split into two- and three-column sections.

4. Keep the period selector visible near the top.
5. Add a prominent quick-add transaction action.
6. Make chart selections navigate to a filtered transaction list.
7. Ensure data refreshes after returning from transaction editing.

Definition of done: the dashboard remains usable at the minimum window width and scales cleanly to large desktop displays.

### Phase 7 — Modernize the supporting pages

#### Transactions

1. Group transactions by date.
2. Add search and filter chips for type, account, category, and date.
3. Color amounts semantically.
4. Add a right-side edit pane on wide screens and a dialog on narrow screens.
5. Add confirmation and undo behavior for deletion.
6. Support keyboard shortcuts such as `Ctrl+N`, `Ctrl+F`, and `Delete`.

#### Accounts

1. Replace the flat list with account cards.
2. Show total balance and account allocation.
3. Add an account-detail page containing balance trend and transactions.
4. Prevent accidental cascade deletion, or clearly warn what will be deleted.

#### Categories

1. Use a wrapping card grid.
2. Add a proper icon and color picker.
3. Show monthly spending and transaction count.
4. Separate expense and income categories visually.

#### Settings

1. Add live theme/accent previews.
2. Add currency and locale settings.
3. Add backup/restore and JSON export.
4. Keep CSV export, but use a file picker instead of silently writing to the data folder.

### Phase 8 — Budget functionality

Once the reporting and card infrastructure is stable:

1. Add a `Budget` entity:

```text
Id
CategoryId
Amount
PeriodType
StartDate
RolloverEnabled
```

2. Add budget CRUD and validation.
3. Calculate spent, remaining, and projected spending.
4. Display `BudgetProgressCard` controls on the dashboard.
5. Add warning thresholds at 75%, 90%, and 100%.
6. Add a budget-versus-actual report.

This phase makes the product a true budget tracker rather than only an expense tracker.

### Phase 9 — Later Flow-like capabilities

Implement separately after the visual/reporting release:

1. Real account-to-account transfers represented by two linked ledger entries.
2. Recurring transactions.
3. Tags and notes search.
4. JSON/ZIP backup and restore.
5. CSV import wizard with preview and duplicate detection.
6. Multi-currency accounts and exchange-rate snapshots.
7. Receipt/file attachments.
8. URI activation for quick transaction creation.
9. PDF reports.

Do not combine these into the chart/theme redesign; they introduce substantial schema and workflow complexity.

### Phase 10 — Quality and release checks

For every phase:

- Unit-test finance calculations with decimal values.
- Test database migration from the existing `budgetbook.db`.
- Verify keyboard navigation and screen-reader labels.
- Check light, dark, high contrast, and 125–200% display scaling.
- Test empty, small, and large transaction datasets.
- Measure dashboard load time and avoid repeated per-account database queries.
- Build and test x64 and ARM64 MSIX packages.
- Run the Windows App Certification Kit before Store submission.

## Suggested delivery sequence

A practical implementation order is:

1. Foundation and migrations.
2. Theme tokens.
3. Reusable cards.
4. Report service.
5. Charts.
6. Dashboard redesign.
7. Transactions/accounts/categories redesign.
8. Budgets.
9. Backup/import/recurring and other Flow-like capabilities.

The first six stages should produce the visual transformation you are expecting without destabilizing the existing app. A reasonable initial target is roughly **10–15 focused development days** for the theme, components, reports, charts, and responsive dashboard; full Flow-like functional parity is a much larger multi-release effort.
