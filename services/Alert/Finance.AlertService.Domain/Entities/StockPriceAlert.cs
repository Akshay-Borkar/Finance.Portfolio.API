using Finance.AlertService.Domain.Common;

namespace Finance.AlertService.Domain.Entities;

public enum AlertCondition { Above, Below }

public class StockPriceAlert : BaseEntity
{
    public Guid UserId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public AlertCondition Condition { get; set; }
    public decimal TargetPrice { get; set; }
    public bool IsTriggered { get; set; }
}
