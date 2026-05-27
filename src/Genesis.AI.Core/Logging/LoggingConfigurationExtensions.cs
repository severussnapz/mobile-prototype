using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Hosting;

namespace Genesis.AI.Core.Logging;

public static class LoggingConfigurationExtensions
{
    public static void ConfigureSerilog(this ConfigureHostBuilder builder)
    {
        if (Environment.GetEnvironmentVariable("HOSTING_ENVIRONMENT") != "AWS")
        {
            return;
        }

        builder.UseSerilog(
            (context, services, configuration) =>
                configuration
                    .MinimumLevel.Is(LogEventLevel.Error)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("environment", Environment.GetEnvironmentVariable("DT_RELEASE_STAGE"))
                    .Enrich.WithProperty("product", Environment.GetEnvironmentVariable("DT_RELEASE_PRODUCT"))
                    .Enrich.WithProperty("version", Environment.GetEnvironmentVariable("DT_RELEASE_VERSION"))
                    .WriteTo.Console(
                        formatter: new DynatraceTextFormatter()));
    }
}
