using Finance.PortfolioService.Application.Contracts.Persistence;
using FluentValidation;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.CreateStockSector;

public class CreateStockSectorCommandValidator : AbstractValidator<CreateStockSectorCommand>
{
    private readonly IStockSectorRepository _stockSectorRepository;

    public CreateStockSectorCommandValidator(IStockSectorRepository stockSectorRepository)
    {
        _stockSectorRepository = stockSectorRepository;

        RuleFor(p => p.StockSectorName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

        RuleFor(p => p.SectorPE)
            .GreaterThan(0).WithMessage("{PropertyName} must be greater than 0.")
            .When(p => p.SectorPE.HasValue);

        RuleFor(c => c)
            .MustAsync(StockSectorNameUnique).WithMessage("Stock Sector already exists.");
    }

    private Task<bool> StockSectorNameUnique(CreateStockSectorCommand command, CancellationToken token)
        => _stockSectorRepository.IsUniqueStockSector(command.StockSectorName);
}
