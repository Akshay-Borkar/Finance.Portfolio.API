using Azure;
using Azure.Search.Documents;
using Finance.MarketDataService.API.Protos;
using Finance.PortfolioService.Application.Contracts.AI;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Infrastructure.AI;
using Finance.PortfolioService.Infrastructure.Constants;
using Finance.PortfolioService.Infrastructure.GrpcClients;
using Finance.SharedKernel.Logging.Messaging;
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

        services.AddMassTransit(x =>
        {
            // Transport is selected at startup based on configuration: RabbitMq:Host wins when
            // present (local/dev via docker-compose), otherwise falls back to Azure Service Bus
            // (staging/prod). Both branches stay wired so switching is a config change, not a code change.
            var rabbitMqHost = configuration["RabbitMq:Host"];
            if (!string.IsNullOrWhiteSpace(rabbitMqHost))
            {
                // RabbitMQ configuration
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(rabbitMqHost, h =>
                    {
                        h.Username(configuration["RabbitMq:Username"] ?? "guest");
                        h.Password(configuration["RabbitMq:Password"] ?? "guest");
                    });
                    cfg.UseCorrelationLogging(ctx);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                // Azure Service Bus configuration
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    var connectionString = configuration[PortfolioInfrastructureConstants.Config.ServiceBusConnectionString];
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new InvalidOperationException(
                            "Neither RabbitMq:Host nor ServiceBusConnectionString is configured. Set one to enable messaging.");

                    cfg.Host(connectionString);
                    cfg.UseCorrelationLogging(ctx);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

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
