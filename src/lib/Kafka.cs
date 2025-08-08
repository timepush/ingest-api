using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.Producers;
using KafkaFlow.Serializer;
using Lib.ServerTiming;
using Microsoft.Extensions.Options;

namespace Timepush.Ingest.Lib;

public class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
  public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      => DateTimeOffset.Parse(reader.GetString()!);

  public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
      => writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));
}

public static class Kafka
{
  public static string ProducerName => "timepush-ingest-api";
  public static IServiceCollection AddKafkaProducer(this IServiceCollection services, IConfiguration configuration)
  {
    var serializerOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
    serializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());

    var topic = configuration["KAFKA_TOPIC_NAME"] ?? String.Empty;
    var brokers = configuration["KAFKA_BROKERS"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();


    services.AddKafka(kafka => kafka
        .AddCluster(cluster => cluster
            .WithBrokers(brokers)
            .AddProducer(
                ProducerName,
                producer => producer
                    .DefaultTopic(topic)
                    .AddMiddlewares(m => m.AddSerializer(x => new JsonCoreSerializer(serializerOptions)))
            )
        )
    // .AddOpenTelemetryInstrumentation()
    );



    services.AddScoped<KafkaMessageSender>();

    return services;
  }
}

public class KafkaMessageSender
{
  private readonly IProducerAccessor _producerAccessor;
  private readonly IServerTiming _serverTiming;
  private readonly AppSettings _appSettings;

  public KafkaMessageSender(IProducerAccessor producerAccessor, IServerTiming serverTiming, IOptions<AppSettings> options)
  {
    _producerAccessor = producerAccessor;
    _serverTiming = serverTiming;
    _appSettings = options.Value;

  }

  public async Task SendMessageAsync<T>(T message)
  {
    var producer = _producerAccessor.GetProducer(Kafka.ProducerName);
    await _serverTiming.TimeAsync("kafka", () => producer.ProduceAsync(_appSettings.Topic, message));
  }

  public async Task SendBatchMessageAsync<T>(IEnumerable<T> messages)
  {
    var producer = _producerAccessor.GetProducer(Kafka.ProducerName);
    await _serverTiming.TimeAsync("kafka", () =>
    {
      var batchItems = messages.Select(msg => new BatchProduceItem(_appSettings.Topic, null, msg, null)).ToList();
      return producer.BatchProduceAsync(batchItems);
    });
  }
}