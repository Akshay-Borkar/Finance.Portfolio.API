using Finance.PortfolioService.Domain.Entities;

namespace Finance.PortfolioService.Application.Contracts.Persistence;

public interface IInvestmentRepository : IGenericRepository<Investment>
{
    Task<List<Investment>> GetInvestmentsByStockId(Guid stockId);
    Task<List<Investment>> GetPortfolioByUserId(Guid userId);
}
