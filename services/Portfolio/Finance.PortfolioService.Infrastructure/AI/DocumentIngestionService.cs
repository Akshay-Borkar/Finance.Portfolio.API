#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Finance.PortfolioService.Application.Contracts.AI;
using Finance.PortfolioService.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;

namespace Finance.PortfolioService.Infrastructure.AI;

public class DocumentIngestionService : IDocumentIngestionService
{
    private const int BatchSize = PortfolioInfrastructureConstants.AI.IngestionBatchSize;

    private readonly DocumentChunkingService _chunker;
    private readonly ITextEmbeddingGenerationService _embedder;
    private readonly SearchClient _searchClient;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        DocumentChunkingService chunker,
        ITextEmbeddingGenerationService embedder,
        SearchClient searchClient,
        ILogger<DocumentIngestionService> logger)
    {
        _chunker = chunker;
        _embedder = embedder;
        _searchClient = searchClient;
        _logger = logger;
    }

    public async Task IngestAsync(Stream pdfStream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ingestion for '{FileName}'.", fileName);

        var chunks = _chunker.ChunkPdf(pdfStream, fileName);
        _logger.LogInformation("'{FileName}' produced {Count} chunks.", fileName, chunks.Count);

        for (int i = 0; i < chunks.Count; i += BatchSize)
        {
            var batch = chunks[i..Math.Min(i + BatchSize, chunks.Count)];

            var texts = batch.Select(c => c.Content).ToList();
            var embeddings = await _embedder.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

            var documents = batch.Zip(embeddings, (chunk, embedding) => new SearchDocument
            {
                ["id"] = chunk.Id,
                ["content"] = chunk.Content,
                ["sourceFile"] = chunk.SourceFile,
                ["pageNumber"] = chunk.PageNumber,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["embedding"] = embedding.ToArray()
            }).ToList();

            await _searchClient.MergeOrUploadDocumentsAsync(documents, cancellationToken: cancellationToken);
            _logger.LogInformation("Upserted batch {Batch}/{Total} for '{FileName}'.",
                i / BatchSize + 1, (int)Math.Ceiling((double)chunks.Count / BatchSize), fileName);
        }

        _logger.LogInformation("Ingestion complete for '{FileName}'.", fileName);
    }
}
