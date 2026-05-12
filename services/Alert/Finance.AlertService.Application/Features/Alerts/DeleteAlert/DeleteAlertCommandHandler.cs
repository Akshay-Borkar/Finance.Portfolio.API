using Finance.AlertService.Application.Contracts.Persistence;
using MediatR;

namespace Finance.AlertService.Application.Features.Alerts.DeleteAlert;

public class DeleteAlertCommandHandler : IRequestHandler<DeleteAlertCommand>
{
    private readonly IStockPriceAlertRepository _alertRepository;

    public DeleteAlertCommandHandler(IStockPriceAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task Handle(DeleteAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId);
        if (alert is null || alert.UserId != request.UserId) return;
        await _alertRepository.DeleteAsync(alert);
    }
}
