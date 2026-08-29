using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class AttachmentService(IDbContextFactory<AppDbContext> contextFactory) : IAttachmentService
{
    private static string AttachmentFolder => Path.Combine(AppDbContext.DbFolder, "attachments");

    public async Task<IReadOnlyList<TransactionAttachment>> GetAttachmentsAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TransactionAttachments.AsNoTracking().Where(item => item.TransactionRecordId == transactionId).OrderByDescending(item => item.AddedAt).ToListAsync(cancellationToken);
    }

    public async Task<TransactionAttachment> AddAttachmentAsync(int transactionId, string sourcePath, CancellationToken cancellationToken = default)
    {
        var source = Path.GetFullPath(sourcePath);
        if (!File.Exists(source)) throw new FileNotFoundException("Attachment file was not found.", source);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Transactions.AnyAsync(item => item.Id == transactionId, cancellationToken)) throw new InvalidOperationException("Save the transaction before attaching a file.");
        Directory.CreateDirectory(AttachmentFolder);
        var safeName = $"{transactionId}-{Guid.NewGuid():N}{Path.GetExtension(source)}";
        var destination = Path.Combine(AttachmentFolder, safeName);
        await using (var input = File.OpenRead(source))
        await using (var output = File.Create(destination)) await input.CopyToAsync(output, cancellationToken);
        var attachment = new TransactionAttachment { TransactionRecordId = transactionId, FileName = Path.GetFileName(source), StoredPath = destination, AddedAt = DateTime.Now };
        db.TransactionAttachments.Add(attachment); await db.SaveChangesAsync(cancellationToken); return attachment;
    }

    public async Task DeleteAttachmentAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.TransactionAttachments.FindAsync([id], cancellationToken);
        if (item is null) return;
        db.TransactionAttachments.Remove(item); await db.SaveChangesAsync(cancellationToken);
        var path = Path.GetFullPath(item.StoredPath);
        var folder = Path.GetFullPath(AttachmentFolder) + Path.DirectorySeparatorChar;
        if (path.StartsWith(folder, StringComparison.OrdinalIgnoreCase) && File.Exists(path)) File.Delete(path);
    }
}
