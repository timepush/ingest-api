using System.Text.Json;
using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.Serializer;
using Microsoft.Extensions.Options;
using Timepush.IngestApi.Lib;

namespace Timepush.IngestApi.Configurations;

public static class KafkaConfiguration
{
  public static WebApplicationBuilder ConfigureKafka(this WebApplicationBuilder builder)
  {
    var kafkaBrokers = builder.Configuration.GetOption<string>("KAFKA_BROKERS");
    var kafkaTopic = builder.Configuration.GetOption<string>("KAFKA_TOPIC_NAME");
    var kafkaLingerMs = builder.Configuration.GetOption<int>("KAFKA_LINGER_MS");

    builder.Services.AddSingleton<KafkaMessageSender>();

    builder.Services.AddKafka(kafka => kafka
            .AddCluster(cluster => cluster
                .WithBrokers(kafkaBrokers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .AddProducer("producer", producer => producer
                    .DefaultTopic(kafkaTopic)
                    .WithAcks(Acks.Leader)  // acks=1
                    .WithCompression(Confluent.Kafka.CompressionType.Lz4) // or Zstd if available
                    .WithLingerMs(kafkaLingerMs)        // small linger for micro-batching
                                                        // .WithBatchSize(256 * 1024)  // 256KB
                    .AddMiddlewares(m => m.AddSerializer(x => new JsonCoreSerializer(JsonDefaults.SerializerOptions)))
                )
            )
        );

    return builder;
  }
}


public sealed class KafkaMessageSender
{
  private readonly IProducerAccessor _producerAccessor;
  private readonly AppOptions _options;

  public KafkaMessageSender(IProducerAccessor producerAccessor, IOptions<AppOptions> options)
  {
    _producerAccessor = producerAccessor;
    _options = options.Value;
  }

  public Task SendBatchMessageAsync<T>(IReadOnlyList<T> messages, Func<T, string?>? keySelector = null, CancellationToken ct = default)
  {
    var producer = _producerAccessor.GetProducer("producer");
    var items = messages.Select(msg => new BatchProduceItem(_options.Topic, keySelector?.Invoke(msg), msg, null)).ToArray();
    return producer.BatchProduceAsync(items);
  }
}