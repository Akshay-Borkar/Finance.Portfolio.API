namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.GetInvestmentsByStockId;

public class InvestmentHistoryDto
{
    public Guid Id { get; set; }
    public double InvestedAmount { get; set; }
    public double BuyingPrice { get; set; }
    public double Quantity { get; set; }
    public DateTime InvestmentDate { get; set; }
}
