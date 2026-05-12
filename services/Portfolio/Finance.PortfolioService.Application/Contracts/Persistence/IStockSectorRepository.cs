using Finance.PortfolioService.Domain.Entities;

namespace Finance.PortfolioService.Application.Contracts.Persistence;

public interface IStockSectorRepository : IGenericRepository<StockSector>
{
    Task<bool> IsUniqueStockSector(string stockSectorName);
}
