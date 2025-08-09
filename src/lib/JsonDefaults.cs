using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Timepush.IngestApi.Lib;

public static class JsonDefaults
{
  public static readonly JsonSerializerOptions SerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Converters = { new UtcDateTimeOffsetConverter() }
  };
}

public class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
  public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      => DateTimeOffset.Parse(reader.GetString()!);

  public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
      => writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'"));
}


