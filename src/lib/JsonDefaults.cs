using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Timepush.IngestApi.Lib;

public static class JsonDefaults2
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

internal static class JsonDefaults
{
  public static readonly JsonSerializerOptions SerializerOptions = Create();

  private static JsonSerializerOptions Create()
  {
    var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
      WriteIndented = false,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // 1) Converters first (highest precedence)
    o.Converters.Insert(0, new UtcDateTimeOffsetConverter());

    // 2) Then your source-generated resolver for speed
    o.TypeInfoResolverChain.Insert(0, IngestJsonContext.Default);

    return o;
  }
}

public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
  private const string FmtMs = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
  private const string FmtSec = "yyyy-MM-dd'T'HH:mm:ss'Z'";
  private static readonly string[] AcceptFormats = new[] { FmtMs, FmtSec };

  public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.String)
      throw new JsonException("Expected string for date-time.");

    var s = reader.GetString();
    if (string.IsNullOrWhiteSpace(s) || s[^1] != 'Z') // must be explicit UTC with 'Z'
      throw new JsonException("Timestamp must end with 'Z'.");

    if (!DateTimeOffset.TryParseExact(
            s,
            AcceptFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var dto))
    {
      throw new JsonException($"Invalid UTC timestamp. Expected {FmtSec} or {FmtMs}.");
    }

    return dto.ToUniversalTime();
  }

  public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
  {
    // Always emit with millisecond precision and 'Z'
    var utc = value.ToUniversalTime().UtcDateTime;

    // "2025-08-13T10:22:31.418Z" => 24 chars
    Span<char> buf = stackalloc char[24];
    if (utc.TryFormat(buf, out var written, FmtMs, CultureInfo.InvariantCulture))
    {
      writer.WriteStringValue(buf[..written]);
    }
    else
    {
      // Fallback (very unlikely)
      writer.WriteStringValue(utc.ToString(FmtMs, CultureInfo.InvariantCulture));
    }
  }
}
