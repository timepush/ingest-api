using System.Threading.Channels;
using Timepush.IngestApi.Features.Ingest;
using Timepush.IngestApi.Features.Ingest.Raw;
using Timepush.IngestApi.Lib;

namespace Timepush.IngestApi.Configurations;

public static class IngestEndpointsConfiguration
{
  public static WebApplicationBuilder ConfigureIngestEndpoints(this WebApplicationBuilder builder)
  {
    var ch = Channel.CreateBounded<RawRequest>(new BoundedChannelOptions(100_000)
    {
      SingleReader = true,
      SingleWriter = false,
      FullMode = BoundedChannelFullMode.Wait
    });
    builder.Services.AddSingleton(ch);
    builder.Services.AddSingleton<ChannelWriter<RawRequest>>(ch.Writer);
    builder.Services.AddSingleton<ChannelReader<RawRequest>>(ch.Reader);

    // Publisher the HTTP/grpc handlers will use
    builder.Services.AddSingleton<IAsyncBatchPublisher<RawRequest>, ChannelBatchPublisher<RawRequest>>();

    // Background flusher to Kafka (adapt to your sender)
    builder.Services.AddSingleton<Func<IReadOnlyList<RawRequest>, CancellationToken, Task>>(sp =>
    {
      var kafka = sp.GetRequiredService<KafkaMessageSender>();      // your existing sender
      return (batch, ct) => kafka.SendBatchMessageAsync(batch, x => x.DataSourceId, ct);
    });
    builder.Services.AddHostedService<KafkaFlushWorker<RawRequest>>();

    builder.Services.AddScoped<IngestAuthFilter>();
    return builder;
  }
}
