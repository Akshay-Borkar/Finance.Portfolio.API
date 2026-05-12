using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Finance.PortfolioService.Persistence.Repositories;

public class StockRepository : GenericRepository<Stock>, IStockRepository
{
    public StockRepository(PortfolioDbContext context) : base(context) { }

    public async Task<Stock?> GetStockByName(string stockName)
        => await _context.Stocks.Include(x => x.StockSector).FirstOrDefaultAsync(x => x.StockName == stockName);

    public async Task<List<Stock>> GetStocksBySectorId(Guid sectorId)
        => await _context.Stocks.Include(x => x.StockSector).Where(x => x.StockSectorId == sectorId).ToListAsync();

    public async Task<List<Stock>> GetStocksByUserId(Guid userId)
        => await _context.Stocks.Include(x => x.StockSector).Where(x => x.UserId == userId).ToListAsync();
}
