using Azure;
using Azure.Search.Documents;
using Finance.MarketDataService.API.Protos;
using Finance.PortfolioService.Application.Contracts.AI;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Infrastructure.AI;
using Finance.PortfolioService.Infrastructure.Constants;
using Finance.PortfolioService.Infrastructure.GrpcClients;
using Finance.SharedKernel.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental
#pragma warning disable SKEXP0010 // AddAzureOpenAITextEmbeddingGeneration is experimental
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Finance.PortfolioService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var marketDataGrpcAddress = configuration[PortfolioInfrastructureConstants.Config.MarketDataGrpcAddress] ?? PortfolioInfrastructureConstants.Config.DefaultGrpcAddress;

        services.AddGrpcClient<MarketDataGrpc.MarketDataGrpcClient>(o =>
        {
            o.Address = new Uri(marketDataGrpcAddress);
        });

        services.AddScoped<IMarketDataGrpcClient, MarketDataGrpcClient>();

        services.AddSharedMessaging(configuration);

        services.Configure<AzureOpenAISettings>(configuration.GetSection(PortfolioInfrastructureConstants.Config.AzureOpenAISection));
        services.Configure<AzureSearchSettings>(configuration.GetSection(PortfolioInfrastructureConstants.Config.AzureSearchSection));

        var searchSettings = configuration.GetSection(PortfolioInfrastructureConstants.Config.AzureSearchSection).Get<AzureSearchSettings>();
        if (searchSettings?.IsConfigured == true)
        {
            services.AddSingleton(new SearchClient(
                new Uri(searchSettings.Endpoint),
                searchSettings.IndexName,
                new AzureKeyCredential(searchSettings.AdminKey)));
        }

        var aiSettings = configuration.GetSection(PortfolioInfrastructureConstants.Config.AzureOpenAISection).Get<AzureOpenAISettings>();
        if (aiSettings?.IsConfigured == true)
        {
            var kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: aiSettings.DeploymentName,
                    endpoint: aiSettings.Endpoint,
                    apiKey: aiSettings.ApiKey)
                .AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: aiSettings.EmbeddingDeploymentName,
                    endpoint: aiSettings.Endpoint,
                    apiKey: aiSettings.ApiKey)
                .Build();

            services.AddSingleton(kernel);

            // Expose SK services for direct injection into infrastructure classes
            services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
                sp.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>());
            services.AddSingleton<IChatCompletionService>(sp =>
                sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());

            services.AddScoped<IPortfolioChatService, PortfolioChatService>();
            services.AddScoped<IRebalancingAgentService, RebalancingAgentService>();
            services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
            services.AddSingleton<DocumentChunkingService>();
            services.AddSingleton<DocumentSearchPlugin>();
        }

        services.AddHostedService<SearchIndexInitializer>();

        return services;
    }
}
