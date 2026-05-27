#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
using Finance.AgentService.Infrastructure.Clients;
using Finance.AgentService.Infrastructure.Plugins;
using Finance.AgentService.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Finance.AgentService.Infrastructure.Orchestration;

public interface IAgentCache
{
    Task<CachedAgents> GetOrCreateAsync(CancellationToken ct = default);
}

public sealed class AgentCache : IAgentCache, IAsyncDisposable
{
    private readonly AzureAISettings _settings;
    private readonly IMarketAuxClient _marketAux;
    private readonly ISentimentApiClient _sentiment;
    private readonly ILogger<AgentCache> _logger;
    private volatile CachedAgents? _cached;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AgentCache(
        AzureAISettings settings,
        IMarketAuxClient marketAux,
        ISentimentApiClient sentiment,
        ILogger<AgentCache> logger)
    {
        _settings = settings;
        _marketAux = marketAux;
        _sentiment = sentiment;
        _logger = logger;
    }

    public async Task<CachedAgents> GetOrCreateAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return _cached;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_cached is not null) return _cached;

            _logger.LogInformation("Provisioning ChatCompletion agents...");

            var autoInvoke = new KernelArguments(
                new AzureOpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() });

            _cached = new CachedAgents(
                BuildAgent(AgentNames.News, PortfolioReviewOrchestrator.NewsAgentInstructions, autoInvoke,
                    KernelPluginFactory.CreateFromObject(new NewsKernelPlugin(_marketAux), "NewsPlugin")),
                BuildAgent(AgentNames.Risk, PortfolioReviewOrchestrator.RiskAgentInstructions, autoInvoke,
                    KernelPluginFactory.CreateFromObject(new SentimentKernelPlugin(_sentiment), "SentimentPlugin")),
                BuildAgent(AgentNames.Report, PortfolioReviewOrchestrator.ReportAgentInstructions),
                BuildAgent(AgentNames.Orchestrator, PortfolioReviewOrchestrator.OrchestratorAgentInstructions));

            _logger.LogInformation("ChatCompletion agents provisioned successfully.");
            return _cached;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private ChatCompletionAgent BuildAgent(string name, string instructions,
        KernelArguments? arguments = null, params KernelPlugin[] plugins)
    {
        var builder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_settings.DeploymentName, _settings.Endpoint, _settings.ApiKey);

        var kernel = builder.Build();
        foreach (var plugin in plugins)
            kernel.Plugins.Add(plugin);

        return new ChatCompletionAgent
        {
            Name = name,
            Instructions = instructions,
            Kernel = kernel,
            Arguments = arguments
        };
    }

    public async ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        await ValueTask.CompletedTask;
    }
}

public sealed record CachedAgents(
    ChatCompletionAgent News,
    ChatCompletionAgent Risk,
    ChatCompletionAgent Report,
    ChatCompletionAgent Orchestrator);
