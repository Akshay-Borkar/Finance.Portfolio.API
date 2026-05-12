using Finance.PortfolioService.Application.Common;
using Finance.PortfolioService.Application.Contracts.Persistence;
using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.GetInvestmentsByStockId;

public class GetInvestmentsByStockIdQueryHandler
    : IRequestHandler<GetInvestmentsByStockIdQuery, PagedResult<InvestmentHistoryDto>>
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IStockRepository _stockRepository;

    public GetInvestmentsByStockIdQueryHandler(
        IInvestmentRepository investmentRepository,
        IStockRepository stockRepository)
    {
        _investmentRepository = investmentRepository;
        _stockRepository = stockRepository;
    }

    public async Task<PagedResult<InvestmentHistoryDto>> Handle(
        GetInvestmentsByStockIdQuery request,
        CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdAsync(request.StockId);
        if (stock is null || stock.UserId != request.UserId)
            return new PagedResult<InvestmentHistoryDto> { Page = request.Page, PageSize = request.PageSize };

        var investments = await _investmentRepository.GetInvestmentsByStockId(request.StockId);

        var ordered = investments.OrderByDescending(i => i.InvestmentDate).ToList();
        var totalCount = ordered.Count;

        var items = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i =>
            {
                double invested = (double)i.InvestedAmount;
                double qty = i.BuyingPrice > 0 ? invested / i.BuyingPrice : 0;
                return new InvestmentHistoryDto
                {
                    Id = i.Id,
                    InvestedAmount = Math.Round(invested, 2),
                    BuyingPrice = Math.Round(i.BuyingPrice, 2),
                    Quantity = Math.Round(qty, 4),
                    InvestmentDate = i.InvestmentDate
                };
            })
            .ToList();

        return new PagedResult<InvestmentHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
