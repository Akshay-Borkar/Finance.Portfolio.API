namespace Finance.PortfolioService.Application.Contracts.AI;

public interface IDocumentIngestionService
{
    Task IngestAsync(Stream pdfStream, string fileName, CancellationToken cancellationToken = default);
}
