using Azure.Monitor.OpenTelemetry.AspNetCore;
using Finance.SharedKernel.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Finance.SharedKernel.Telemetry;

/// <summary>
/// Wires requests/dependencies/exceptions/distributed traces to Azure Application Insights via
/// the Azure Monitor OpenTelemetry Distro. Deliberately independent of Finance.SharedKernel.Logging
/// (Serilog/Seq) — App Insights carries telemetry, not application logs. No-ops when
/// ApplicationInsights:Enabled is false or ApplicationInsights:ConnectionString is unset, so local
/// dev can flip one flag to stop sending data without losing the connection string, and works
/// without an Azure resource at all by default.
/// </summary>
public static class TelemetryExtensions
{
    public static WebApplicationBuilder AddSharedTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        var enabled = builder.Configuration.GetValue(AuthConstants.Config.AppInsightsEnabled, defaultValue: true);
        var connectionString = builder.Configuration[AuthConstants.Config.AppInsightsConnectionString];
        if (!enabled || string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddSource("MassTransit")
                .AddEntityFrameworkCoreInstrumentation())
            .UseAzureMonitor(options => options.ConnectionString = connectionString);

        return builder;
    }
}
