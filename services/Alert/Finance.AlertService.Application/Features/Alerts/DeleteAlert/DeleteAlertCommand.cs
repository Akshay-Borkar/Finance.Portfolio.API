using MediatR;

namespace Finance.AlertService.Application.Features.Alerts.DeleteAlert;

public record DeleteAlertCommand(Guid AlertId, Guid UserId) : IRequest;
