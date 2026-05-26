using System.Runtime.CompilerServices;
using Finance.PortfolioService.Application.Contracts.AI;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Finance.PortfolioService.Infrastructure.AI;

public class PortfolioChatService : IPortfolioChatService
{
    private readonly Kernel _kernelTemplate;
    private readonly IMediator _mediator;
    private readonly DocumentSearchPlugin? _documentSearchPlugin;
    private readonly ILogger<PortfolioChatService> _logger;

    public PortfolioChatService(
        Kernel kernelTemplate,
        IMediator mediator,
        ILogger<PortfolioChatService> logger,
        DocumentSearchPlugin? documentSearchPlugin = null)
    {
        _kernelTemplate = kernelTemplate;
        _mediator = mediator;
        _logger = logger;
        _documentSearchPlugin = documentSearchPlugin;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string userMessage,
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var kernel = _kernelTemplate.Clone();
        kernel.Plugins.AddFromObject(new PortfolioPlugin(_mediator, userId), "Portfolio");

        if (_documentSearchPlugin is not null)
        {
            kernel.Plugins.AddFromObject(_documentSearchPlugin, "DocumentSearch");
            _logger.LogDebug("DocumentSearchPlugin registered on kernel for this request.");
        }

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            """
            You are a knowledgeable and friendly financial advisor assistant specializing in the Indian stock market.
            You help users understand their portfolio performance, analyze their holdings, and make sense of their investments.

            Important guidelines:
            - Always fetch the portfolio data using the available functions before answering any portfolio-related question.
            - If the user asks about content from uploaded documents — such as annual reports, company financials, earnings results,
              dividend history, risk factors, management guidance, or any company-specific information from PDFs — use the
              search_financial_documents function to retrieve the answer from the indexed documents.
            - All monetary values are in Indian Rupees (INR, ₹).
            - Provide clear, concise, and actionable insights based on the portfolio data.
            - You are NOT a SEBI-registered investment advisor. Always remind users that your analysis is for informational purposes only and not financial advice.
            - When discussing P&L, clearly indicate whether it is a gain or a loss.
            """);
        chatHistory.AddUserMessage(userMessage);

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        await foreach (var update in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Content))
                yield return update.Content;
        }
    }
}
