using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Timepush.IngestApi.Features.Ingest.Raw;



public sealed class RawRequest
{
  [JsonPropertyName("timestamp")]
  public DateTimeOffset Timestamp { get; set; } // converter from JsonDefaults handles Z-format

  [JsonPropertyName("value")]
  public double Value { get; set; }

  [JsonPropertyName("is_valid")]
  public bool IsValid { get; set; } = true;     // default true when missing

  [JsonPropertyName("data_source_id")]
  public string? DataSourceId { get; set; }

  [JsonPropertyName("metadata")]
  public JsonObject? Metadata { get; set; }
}


[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
  PropertyNameCaseInsensitive = true,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RawRequest))]
internal partial class RawJsonContext : JsonSerializerContext { }