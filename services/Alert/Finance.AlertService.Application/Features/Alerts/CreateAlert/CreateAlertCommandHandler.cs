using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Domain.Entities;
using MediatR;

namespace Finance.AlertService.Application.Features.Alerts.CreateAlert;

public class CreateAlertCommandHandler : IRequestHandler<CreateAlertCommand, Guid>
{
    private readonly IStockPriceAlertRepository _alertRepository;

    public CreateAlertCommandHandler(IStockPriceAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<Guid> Handle(CreateAlertCommand request, CancellationToken cancellationToken)
    {
        var condition = Enum.Parse<AlertCondition>(request.Condition, ignoreCase: true);
        var alert = new StockPriceAlert
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Ticker = request.Ticker.ToUpperInvariant(),
            UserEmail = request.UserEmail,
            Condition = condition,
            TargetPrice = request.TargetPrice,
            IsTriggered = false
        };
        await _alertRepository.CreateAsync(alert);
        return alert.Id;
    }
}
