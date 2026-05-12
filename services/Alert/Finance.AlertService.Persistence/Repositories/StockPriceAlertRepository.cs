using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Domain.Entities;
using Finance.AlertService.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Finance.AlertService.Persistence.Repositories;

public class StockPriceAlertRepository : GenericRepository<StockPriceAlert>, IStockPriceAlertRepository
{
    public StockPriceAlertRepository(AlertDbContext context) : base(context) { }

    public async Task<List<StockPriceAlert>> GetActiveAlertsByTickerAsync(string ticker)
        => await _context.StockPriceAlerts
            .Where(a => a.Ticker == ticker && !a.IsTriggered)
            .ToListAsync();

    public async Task<List<StockPriceAlert>> GetAlertsByUserIdAsync(Guid userId)
        => await _context.StockPriceAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.DateCreated)
            .ToListAsync();

    public async Task<(List<StockPriceAlert> Items, int TotalCount)> GetAlertsByUserIdPagedAsync(
        Guid userId, int page, int pageSize)
    {
        var query = _context.StockPriceAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.DateCreated);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
