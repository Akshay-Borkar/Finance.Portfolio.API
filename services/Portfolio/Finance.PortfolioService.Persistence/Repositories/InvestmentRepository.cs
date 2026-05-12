using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Finance.PortfolioService.Persistence.Repositories;

public class InvestmentRepository : GenericRepository<Investment>, IInvestmentRepository
{
    public InvestmentRepository(PortfolioDbContext context) : base(context) { }

    public async Task<List<Investment>> GetInvestmentsByStockId(Guid stockId)
        => await _context.Investments.Include(x => x.StockDetails).Where(x => x.StockDetailsId == stockId).ToListAsync();

    public async Task<List<Investment>> GetPortfolioByUserId(Guid userId)
        => await _context.Investments
            .Include(x => x.StockDetails)
                .ThenInclude(s => s.StockSector)
            .Where(x => x.StockDetails.UserId == userId)
            .ToListAsync();
}
