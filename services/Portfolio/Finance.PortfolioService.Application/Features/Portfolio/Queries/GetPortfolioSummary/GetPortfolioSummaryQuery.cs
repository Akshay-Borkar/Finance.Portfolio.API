using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.GetPortfolioSummary;

public record GetPortfolioSummaryQuery(Guid UserId) : IRequest<PortfolioSummaryDto>;
