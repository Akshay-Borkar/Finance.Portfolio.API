using Finance.AlertService.Application.Common;
using MediatR;

namespace Finance.AlertService.Application.Features.Alerts.GetUserAlerts;

public record GetUserAlertsQuery(Guid UserId, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<AlertDto>>;
