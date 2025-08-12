using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Timepush.IngestApi.Features.Ingest.Raw;

public record RawRequest(DateTimeOffset Timestamp, double Value, bool IsValid, string? DataSourceId = null, JsonObject? Metadata = null);