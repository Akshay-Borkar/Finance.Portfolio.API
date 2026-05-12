using Finance.AlertService.Application.Common;
using Finance.AlertService.Application.Contracts.Persistence;
using MediatR;

namespace Finance.AlertService.Application.Features.Alerts.GetUserAlerts;

public class GetUserAlertsQueryHandler : IRequestHandler<GetUserAlertsQuery, PagedResult<AlertDto>>
{
    private readonly IStockPriceAlertRepository _alertRepository;

    public GetUserAlertsQueryHandler(IStockPriceAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<PagedResult<AlertDto>> Handle(GetUserAlertsQuery request, CancellationToken cancellationToken)
    {
        var (alerts, totalCount) = await _alertRepository.GetAlertsByUserIdPagedAsync(
            request.UserId, request.Page, request.PageSize);

        var items = alerts.Select(a => new AlertDto
        {
            Id = a.Id,
            Ticker = a.Ticker,
            Condition = a.Condition.ToString(),
            TargetPrice = a.TargetPrice,
            IsTriggered = a.IsTriggered,
            DateCreated = a.DateCreated
        }).ToList();

        return new PagedResult<AlertDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
