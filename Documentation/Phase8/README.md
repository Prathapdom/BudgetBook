# Phase 8 — Budgets

Phase 8 adds first-class category budgets to BudgetBook.

- `Budget` is stored through the `AddBudgets` EF Core migration.
- One budget can be configured per expense category, with monthly or weekly limits.
- Monthly budgets can carry unused allocation into later months.
- Budget progress reports expose spent, remaining, and projected spending.
- Health changes at 75% (warning), 90% (critical), and 100% (exceeded).
- The dedicated Budgets page supports create, edit, delete, period navigation, and
  budget-versus-actual summary cards.
- The dashboard shows live `BudgetProgressCard` controls and links to budget management.
- JSON backup and restore include budget definitions.

Verification: the full suite contains 19 passing tests, the x64 build completes with
zero warnings, and a fresh isolated database launches through both migrations.
