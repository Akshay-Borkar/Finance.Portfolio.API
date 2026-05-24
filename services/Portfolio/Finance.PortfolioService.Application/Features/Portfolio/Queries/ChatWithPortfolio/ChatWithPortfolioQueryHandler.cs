using Finance.PortfolioService.Application.Contracts.AI;
using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.ChatWithPortfolio;

public class ChatWithPortfolioQueryHandler : IRequestHandler<ChatWithPortfolioQuery, IAsyncEnumerable<string>>
{
    private readonly IPortfolioChatService _chatService;

    public ChatWithPortfolioQueryHandler(IPortfolioChatService chatService)
    {
        _chatService = chatService;
    }

    public Task<IAsyncEnumerable<string>> Handle(ChatWithPortfolioQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_chatService.StreamChatAsync(request.Message, request.UserId, cancellationToken));
    }
}
