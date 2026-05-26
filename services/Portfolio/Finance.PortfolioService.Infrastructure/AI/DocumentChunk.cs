namespace Finance.PortfolioService.Infrastructure.AI;

public record DocumentChunk(
    string Id,
    string Content,
    string SourceFile,
    int PageNumber,
    int ChunkIndex);
