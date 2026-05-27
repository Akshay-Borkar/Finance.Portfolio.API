#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Agents.Chat;

namespace Finance.AgentService.Infrastructure.Orchestration;

/// <summary>
/// Terminates when OrchestratorAgent emits [REVIEW_COMPLETE].
/// MaximumIterations on the base class acts as a hard safety cap.
/// </summary>
internal sealed class PortfolioReviewTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(
        Agent agent,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default)
    {
        var last = history[^1];
        bool done =
            last.Role == AuthorRole.Assistant &&
            last.AuthorName == AgentNames.Orchestrator &&
            (last.Content?.Contains("[REVIEW_COMPLETE]", StringComparison.OrdinalIgnoreCase) ?? false);

        return Task.FromResult(done);
    }
}
