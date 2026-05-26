using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.PortfolioService.Infrastructure.AI;

public class SearchIndexInitializer : IHostedService
{
    private readonly AzureSearchSettings _settings;
    private readonly ILogger<SearchIndexInitializer> _logger;

    public SearchIndexInitializer(IOptions<AzureSearchSettings> settings, ILogger<SearchIndexInitializer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.IsConfigured)
        {
            _logger.LogWarning("Azure AI Search is not configured — skipping index initialisation.");
            return;
        }

        try
        {
            var indexClient = new SearchIndexClient(
                new Uri(_settings.Endpoint),
                new AzureKeyCredential(_settings.AdminKey));

            var index = BuildIndex(_settings.IndexName);
            await indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
            _logger.LogInformation("Azure AI Search index '{Index}' is ready.", _settings.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise Azure AI Search index.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static SearchIndex BuildIndex(string indexName)
    {
        const int EmbeddingDims = 1536;

        var index = new SearchIndex(indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("content") { IsFilterable = false },
                new SimpleField("sourceFile", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true },
                new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsFilterable = true },
                new VectorSearchField("embedding", EmbeddingDims, "hnsw-cosine")
            },
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile("hnsw-cosine", "hnsw-cosine-algo") },
                Algorithms = { new HnswAlgorithmConfiguration("hnsw-cosine-algo")
                {
                    Parameters = new HnswParameters { Metric = VectorSearchAlgorithmMetric.Cosine }
                }}
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("financial-docs-semantic", new SemanticPrioritizedFields
                    {
                        ContentFields = { new SemanticField("content") }
                    })
                }
            }
        };

        return index;
    }
}
