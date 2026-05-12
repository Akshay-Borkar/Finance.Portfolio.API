using MediatR;

namespace Finance.AlertService.Application.Features.Alerts.CreateAlert;

public class CreateAlertCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
}
