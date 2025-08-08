using KafkaFlow.OpenTelemetry;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Timepush.Ingest.Lib;

public static class Observability
{
  public static WebApplicationBuilder ConfigureObservability(this WebApplicationBuilder builder)
  {

    builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TIMEPUSH-INGEST"))
    .WithMetrics(metrics =>
    {
      metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation();

      metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
      tracing
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation();
      // .AddSource(KafkaFlowInstrumentation.ActivitySourceName)
      // .AddRedisInstrumentation()
      // .AddNpgsql();

      tracing.AddOtlpExporter();
    });

    builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());
    return builder;
  }
}