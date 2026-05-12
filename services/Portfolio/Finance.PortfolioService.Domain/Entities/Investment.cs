using Finance.PortfolioService.Domain.Common;

namespace Finance.PortfolioService.Domain.Entities;

public class Investment : BaseEntity
{
    public decimal InvestedAmount { get; set; }
    public double BuyingPrice { get; set; }
    public DateTime InvestmentDate { get; set; }
    public Guid StockDetailsId { get; set; }
    public Stock StockDetails { get; set; } = null!;
}
