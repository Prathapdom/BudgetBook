using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class ExchangeRateService(IDbContextFactory<AppDbContext> contextFactory) : IExchangeRateService
{
    public async Task<IReadOnlyList<ExchangeRateSnapshot>> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ExchangeRates.AsNoTracking().OrderByDescending(rate => rate.CapturedAt).ToListAsync(cancellationToken);
    }

    public async Task SaveRateAsync(ExchangeRateSnapshot rate, CancellationToken cancellationToken = default)
    {
        rate.BaseCurrency = rate.BaseCurrency.Trim().ToUpperInvariant(); rate.QuoteCurrency = rate.QuoteCurrency.Trim().ToUpperInvariant();
        if (rate.BaseCurrency.Length != 3 || rate.QuoteCurrency.Length != 3 || rate.BaseCurrency == rate.QuoteCurrency) throw new InvalidOperationException("Enter two different three-letter currency codes.");
        if (rate.Rate <= 0m) throw new InvalidOperationException("Exchange rate must be greater than zero.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.ExchangeRates.Add(rate); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal?> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime asOf, CancellationToken cancellationToken = default)
    {
        fromCurrency = fromCurrency.ToUpperInvariant(); toCurrency = toCurrency.ToUpperInvariant();
        if (fromCurrency == toCurrency) return amount;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var direct = await db.ExchangeRates.AsNoTracking().Where(rate => rate.BaseCurrency == fromCurrency && rate.QuoteCurrency == toCurrency && rate.CapturedAt <= asOf)
            .OrderByDescending(rate => rate.CapturedAt).FirstOrDefaultAsync(cancellationToken);
        if (direct is not null) return amount * direct.Rate;
        var inverse = await db.ExchangeRates.AsNoTracking().Where(rate => rate.BaseCurrency == toCurrency && rate.QuoteCurrency == fromCurrency && rate.CapturedAt <= asOf)
            .OrderByDescending(rate => rate.CapturedAt).FirstOrDefaultAsync(cancellationToken);
        return inverse is null ? null : amount / inverse.Rate;
    }
}
