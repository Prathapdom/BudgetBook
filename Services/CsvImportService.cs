using System.Globalization;
using System.Text;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class CsvImportService(IDbContextFactory<AppDbContext> contextFactory) : ICsvImportService
{
    public async Task<CsvImportPreview> PreviewAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(sourcePath, cancellationToken);
        var errors = new List<string>(); var parsed = new List<TransactionRecord>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await db.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        for (var lineNumber = 1; lineNumber < lines.Length; lineNumber++)
        {
            var fields = ParseLine(lines[lineNumber]);
            if (fields.Count < 6 || !DateTime.TryParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !Enum.TryParse<TransactionType>(fields[1], true, out var type) || !decimal.TryParse(fields[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            { errors.Add($"Line {lineNumber + 1}: invalid date, type, amount, or column count."); continue; }
            var account = accounts.FirstOrDefault(item => item.Name.Equals(fields[3], StringComparison.OrdinalIgnoreCase));
            if (account is null) { errors.Add($"Line {lineNumber + 1}: account '{fields[3]}' was not found."); continue; }
            var category = categories.FirstOrDefault(item => item.Name.Equals(fields[4], StringComparison.OrdinalIgnoreCase));
            parsed.Add(new TransactionRecord { Date = date, Type = type, Amount = Math.Abs(amount), AccountId = account.Id, CategoryId = category?.Id, Note = fields[5], Tags = fields.Count > 6 ? fields[6] : "" });
        }
        var existing = await db.Transactions.AsNoTracking().ToListAsync(cancellationToken);
        var duplicates = parsed.Count(item => existing.Any(current => current.Date == item.Date && current.Type == item.Type && current.Amount == item.Amount && current.AccountId == item.AccountId && current.Note == item.Note));
        var unique = parsed.Where(item => !existing.Any(current => current.Date == item.Date && current.Type == item.Type && current.Amount == item.Amount && current.AccountId == item.AccountId && current.Note == item.Note)).ToList();
        return new CsvImportPreview(unique, duplicates, errors);
    }

    public async Task<int> ImportAsync(CsvImportPreview preview, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.Transactions.AddRange(preview.Transactions); await db.SaveChangesAsync(cancellationToken); return preview.Transactions.Count;
    }

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>(); var current = new StringBuilder(); var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"' && quoted && index + 1 < line.Length && line[index + 1] == '"') { current.Append('"'); index++; }
            else if (character == '"') quoted = !quoted;
            else if (character == ',' && !quoted) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(character);
        }
        result.Add(current.ToString()); return result;
    }
}
