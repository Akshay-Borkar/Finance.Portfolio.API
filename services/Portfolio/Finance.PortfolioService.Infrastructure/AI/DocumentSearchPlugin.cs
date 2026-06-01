#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental
using System.ComponentModel;
using System.Text;
using Finance.PortfolioService.Infrastructure.Constants;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Finance.PortfolioService.Infrastructure.AI;

public class DocumentSearchPlugin
{
    private const int TopK = PortfolioInfrastructureConstants.AI.SearchTopK;

    private const string SystemPrompt =
        """
        You are a financial document assistant. Answer ONLY using the context chunks provided below.
        Rules (non-negotiable):
        1. Every factual claim must cite its source as [sourceFile, page N].
        2. If the answer cannot be found in the provided context, respond exactly with:
           "The information requested was not found in the indexed documents."
        3. Do not infer, hallucinate, or use knowledge outside the provided context.
        4. Keep answers concise and structured.
        """;

    private readonly ITextEmbeddingGenerationService _embedder;
    private readonly IChatCompletionService _chat;
    private readonly AzureSearchSettings _searchSettings;
    private readonly ILogger<DocumentSearchPlugin> _logger;

    public DocumentSearchPlugin(
        ITextEmbeddingGenerationService embedder,
        IChatCompletionService chat,
        IOptions<AzureSearchSettings> searchSettings,
        ILogger<DocumentSearchPlugin> logger)
    {
        _embedder = embedder;
        _chat = chat;
        _searchSettings = searchSettings.Value;
        _logger = logger;
    }

    [KernelFunction("search_financial_documents")]
    [Description(
        "Searches indexed financial PDF documents and returns a grounded answer with source citations. " +
        "Call this whenever the user asks about content from uploaded documents, including: annual reports, " +
        "quarterly results, earnings, revenue, profit, dividends, risk factors, management guidance, " +
        "company fundamentals, balance sheet, cash flow, debt, or any company-specific information. " +
        "Always prefer this over general knowledge when the user references a specific company or document.")]
    public async Task<string> SearchDocumentsAsync(
        [Description("The user's full question to search for in the financial documents.")] string question)
    {
        _logger.LogInformation("DocumentSearchPlugin invoked with question: {Question}", question);

        if (!_searchSettings.IsConfigured)
        {
            _logger.LogWarning("DocumentSearchPlugin called but AzureSearch is not configured.");
            return "Document search is not configured.";
        }

        var embedding = await _embedder.GenerateEmbeddingAsync(question);

        var searchClient = new SearchClient(
            new Uri(_searchSettings.Endpoint),
            _searchSettings.IndexName,
            new AzureKeyCredential(_searchSettings.AdminKey));

        var searchOptions = new SearchOptions
        {
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(embedding.ToArray())
                    {
                        KNearestNeighborsCount = TopK,
                        Fields = { "embedding" }
                    }
                }
            },
            Size = TopK,
            Select = { "content", "sourceFile", "pageNumber" }
        };

        var response = await searchClient.SearchAsync<SearchDocument>(question, searchOptions);

        var contextBuilder = new StringBuilder();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var doc = result.Document;
            contextBuilder.AppendLine($"[{doc["sourceFile"]}, page {doc["pageNumber"]}]");
            contextBuilder.AppendLine(doc["content"]?.ToString());
            contextBuilder.AppendLine();
        }

        var context = contextBuilder.ToString();
        if (string.IsNullOrWhiteSpace(context))
        {
            _logger.LogInformation("DocumentSearchPlugin: no chunks matched for question: {Question}", question);
            return "The information requested was not found in the indexed documents.";
        }

        _logger.LogInformation("DocumentSearchPlugin: found context, generating grounded answer.");

        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);
        history.AddUserMessage($"Context:\n{context}\n\nQuestion: {question}");

        var reply = await _chat.GetChatMessageContentAsync(history);
        return reply.Content ?? "The information requested was not found in the indexed documents.";
    }
}
