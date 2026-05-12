namespace Finance.AlertService.Application.Features.Alerts;

public class AlertDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public bool IsTriggered { get; set; }
    public DateTime DateCreated { get; set; }
}
