using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.ChatWithPortfolio;

public record ChatWithPortfolioQuery(string Message, Guid UserId) : IRequest<IAsyncEnumerable<string>>;
