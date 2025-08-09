using System.Text.Json.Nodes;

namespace Timepush.Ingest.Features.Ingest.Raw;

public record RawRequest(DateTimeOffset Timestamp, double Value, bool IsValid, string? DataSourceId = null, JsonObject? Metadata = null);