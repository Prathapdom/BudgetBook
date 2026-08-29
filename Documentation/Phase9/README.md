# Phase 9 - Advanced finance workflows

Phase 9 adds the advanced workflows identified in the implementation roadmap.

- Transfers create linked outgoing and incoming ledger entries with a shared group ID.
  Editing updates both entries and deleting either entry deletes the pair.
- Recurring rules support weekly, monthly, and yearly schedules and generate all due
  occurrences automatically.
- Transactions support tags, tag-aware search, and receipt/file attachments stored in
  the local BudgetBook data folder.
- CSV import previews valid rows, reports errors, and skips probable duplicates.
- JSON export now includes Phase 9 data, while portable ZIP backup also carries receipt files.
- Exchange-rate snapshots support direct and inverse historical conversion.
- The `budgetbook://add` protocol opens a prefilled quick-entry transaction on cold start.
- Monthly PDF reports include summary, budget-versus-actual figures, transactions, and
  page numbering.

## URI example

```text
budgetbook://add?amount=42.50&type=Expense&note=Lunch&tags=work
```

Optional query fields: `date`, `accountId`, and `categoryId`.

## Verification

- 23 unit/integration tests pass.
- x64 build succeeds with zero warnings.
- Fresh isolated startup succeeds through all three EF Core migrations.
- The PDF export is validated structurally with `pdfinfo`, rendered with Poppler, and
  visually inspected for clipping, overlap, alignment, and page numbering.
