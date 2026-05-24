using System.Runtime.CompilerServices;
using Finance.PortfolioService.Application.Contracts.AI;
using MediatR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Finance.PortfolioService.Infrastructure.AI;

public class PortfolioChatService : IPortfolioChatService
{
    private readonly Kernel _kernelTemplate;
    private readonly IMediator _mediator;

    public PortfolioChatService(Kernel kernelTemplate, IMediator mediator)
    {
        _kernelTemplate = kernelTemplate;
        _mediator = mediator;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string userMessage,
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var kernel = _kernelTemplate.Clone();
        kernel.Plugins.AddFromObject(new PortfolioPlugin(_mediator, userId), "Portfolio");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            """
            You are a knowledgeable and friendly financial advisor assistant specializing in the Indian stock market.
            You help users understand their portfolio performance, analyze their holdings, and make sense of their investments.

            Important guidelines:
            - Always fetch the portfolio data using the available functions before answering any portfolio-related question.
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
