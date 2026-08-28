# Reports

Chart-ready report models and pure finance calculations live here. Database-backed report queries are implemented behind `IReportService`.

## Report pipeline

1. `ReportFilter` owns an exclusive date range plus optional account and category dimensions.
2. `ReportService` applies those filters in SQLite and projects only fields needed by reports.
3. `FinanceCalculator` performs exact decimal aggregation and fills missing time-series buckets.
4. UI and chart code consume immutable report records rather than EF entities.

Supported periods are this month, last month, the last three or six months, this year, and a custom inclusive date range. Time series can be daily, weekly, monthly, or automatically selected from the range length.
