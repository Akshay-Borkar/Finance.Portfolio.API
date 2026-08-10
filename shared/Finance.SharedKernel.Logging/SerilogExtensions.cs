using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Finance.SharedKernel.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Replaces the default console logger with structured Serilog output: readable text in
    /// Development, compact JSON elsewhere, always enriched with service name, correlation id
    /// (via LogContext, see CorrelationIdMiddleware) and W3C trace/span ids. Optionally ships
    /// to Seq when Seq:Url is configured, so all services' logs land in one searchable place.
    /// </summary>
    public static WebApplicationBuilder AddSharedLogging(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, _, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithProperty("Service", serviceName);

            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({Service}) [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }

            var seqUrl = context.Configuration["Seq:Url"];
            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                configuration.WriteTo.Seq(seqUrl);
            }
        });

        return builder;
    }
}
