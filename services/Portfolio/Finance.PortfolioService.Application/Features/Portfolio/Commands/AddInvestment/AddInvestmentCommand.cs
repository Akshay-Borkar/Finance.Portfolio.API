using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Commands.AddInvestment;

public class AddInvestmentCommand : IRequest<Guid>
{
    public Guid StockId { get; set; }
    public Guid UserId { get; set; }
    public decimal InvestedAmount { get; set; }
    public double BuyingPrice { get; set; }
    public DateTime InvestmentDate { get; set; }
}
