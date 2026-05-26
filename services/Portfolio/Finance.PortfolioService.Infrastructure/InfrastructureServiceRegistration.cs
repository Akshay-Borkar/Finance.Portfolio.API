using Finance.MarketDataService.API.Protos;
using Finance.PortfolioService.Application.Contracts.AI;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Infrastructure.AI;
using Finance.PortfolioService.Infrastructure.GrpcClients;
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
        var marketDataGrpcAddress = configuration["MarketData:GrpcAddress"] ?? "http://marketdata-svc:8081";

        services.AddGrpcClient<MarketDataGrpc.MarketDataGrpcClient>(o =>
        {
            o.Address = new Uri(marketDataGrpcAddress);
        });

        services.AddScoped<IMarketDataGrpcClient, MarketDataGrpcClient>();

        services.AddMassTransit(x =>
        {
            // RabbitMQ configuration
            // x.UsingRabbitMq((ctx, cfg) =>
            // {
            //     cfg.Host(
            //         configuration["RabbitMq:Host"] ?? "rabbitmq",
            //         h =>
            //         {
            //             h.Username(configuration["RabbitMq:Username"] ?? "guest");
            //             h.Password(configuration["RabbitMq:Password"] ?? "guest");
            //         });
            // });

            // Azure Service Bus configuration
            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                var connectionString = configuration["ServiceBusConnectionString"];
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "ServiceBusConnectionString is not configured. Add it to appsettings or user secrets.");

                cfg.Host(connectionString);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));
        services.Configure<AzureSearchSettings>(configuration.GetSection("AzureSearch"));

        var aiSettings = configuration.GetSection("AzureOpenAI").Get<AzureOpenAISettings>();
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
