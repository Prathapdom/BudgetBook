using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class CategoryService(IDbContextFactory<AppDbContext> contextFactory) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (category.Id == 0) db.Categories.Add(category);
        else db.Categories.Update(category);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.FindAsync([id], cancellationToken);
        if (category is null) return;
        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }
}
