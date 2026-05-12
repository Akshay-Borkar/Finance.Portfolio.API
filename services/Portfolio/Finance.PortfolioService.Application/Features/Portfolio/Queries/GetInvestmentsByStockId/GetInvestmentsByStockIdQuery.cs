using Finance.PortfolioService.Application.Common;
using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.GetInvestmentsByStockId;

public record GetInvestmentsByStockIdQuery(Guid StockId, Guid UserId, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<InvestmentHistoryDto>>;
