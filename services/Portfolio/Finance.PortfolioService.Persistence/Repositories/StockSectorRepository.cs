using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Finance.PortfolioService.Persistence.Repositories;

public class StockSectorRepository : GenericRepository<StockSector>, IStockSectorRepository
{
    public StockSectorRepository(PortfolioDbContext context) : base(context) { }

    public async Task<bool> IsUniqueStockSector(string stockSectorName)
        => !await _context.StockSectors.AnyAsync(x => x.StockSectorName == stockSectorName);
}
