using System.Globalization;
using System.Text;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.Services;

public sealed class PdfReportService(IReportService reports, ITransactionService transactions, IBudgetService budgets) : IPdfReportService
{
    public async Task<string> ExportMonthlyReportAsync(string destinationPath, DateTime month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(month.Year, month.Month, 1); var filter = new ReportFilter(start, start.AddMonths(1));
        var summaryTask = reports.GetSummaryAsync(filter, cancellationToken);
        var transactionTask = transactions.GetTransactionsAsync(start, cancellationToken: cancellationToken);
        var budgetTask = budgets.GetBudgetReportAsync(filter, cancellationToken: cancellationToken);
        await Task.WhenAll(summaryTask, transactionTask, budgetTask);
        var summary = await summaryTask; var items = await transactionTask; var budget = await budgetTask;
        var pages = new List<List<string>>(); var page = NewPage(start, summary, budget); pages.Add(page);
        foreach (var item in items)
        {
            if (page.Count >= 38) { page = NewContinuationPage(start); pages.Add(page); }
            page.Add($"{item.Date:yyyy-MM-dd}  {Trim(item.CategoryName, 18),-18}  {Trim(item.AccountName, 16),-16}  {item.SignedAmount,12:N2}  {Trim(item.Note, 24)}");
        }
        if (items.Count == 0) page.Add("No transactions in this period.");
        await File.WriteAllBytesAsync(destinationPath, BuildPdf(pages), cancellationToken);
        return destinationPath;
    }

    private static List<string> NewPage(DateTime month, DashboardSummary summary, BudgetVsActualReport budget) =>
    [
        $"BudgetBook - {month:MMMM yyyy}", "Monthly finance report", "",
        $"Income: {summary.Income:N2}    Expenses: {summary.Expense:N2}    Net: {summary.Net:N2}",
        $"Budgeted: {budget.TotalBudget:N2}    Budget spent: {budget.TotalSpent:N2}    Projected: {budget.TotalProjected:N2}",
        "", "Date        Category            Account                 Amount  Note", new string('-', 92),
    ];
    private static List<string> NewContinuationPage(DateTime month) => [$"BudgetBook - {month:MMMM yyyy} (continued)", "", "Date        Category            Account                 Amount  Note", new string('-', 92)];
    private static string Trim(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "~";
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ").Replace("\n", " ");

    private static byte[] BuildPdf(IReadOnlyList<List<string>> pages)
    {
        var objects = new List<byte[]>();
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        var pageObjectNumbers = Enumerable.Range(0, pages.Count).Select(index => 4 + index * 2).ToArray();
        objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pages.Count} >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"));
        for (var index = 0; index < pages.Count; index++)
        {
            var contentNumber = 5 + index * 2;
            objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentNumber} 0 R >>"));
            var commands = new StringBuilder("BT\n/F1 9 Tf\n45 748 Td\n12 TL\n");
            foreach (var line in pages[index]) commands.Append('(').Append(Escape(line)).Append(") Tj\nT*\n");
            commands.Append("ET\nBT\n/F1 8 Tf\n270 28 Td\n(Page ").Append(index + 1).Append(" of ").Append(pages.Count).Append(") Tj\nET\n");
            var content = Encoding.ASCII.GetBytes(commands.ToString());
            objects.Add(Encoding.ASCII.GetBytes($"<< /Length {content.Length} >>\nstream\n{commands}endstream"));
        }
        using var output = new MemoryStream(); using var writer = new StreamWriter(output, Encoding.ASCII, 1024, true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n%âãÏÓ\n"); writer.Flush(); var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++) { offsets.Add(output.Position); writer.Write($"{index + 1} 0 obj\n"); writer.Flush(); output.Write(objects[index]); writer.Write("\nendobj\n"); writer.Flush(); }
        var xref = output.Position; writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"); writer.Flush(); return output.ToArray();
    }
}
