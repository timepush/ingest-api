using System.Text.Json.Serialization;
using Timepush.IngestApi.Features.Ingest.Raw;

namespace Timepush.IngestApi.Lib;

// Single source of truth for JSON shape + perf
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(RawRequest))]
[JsonSerializable(typeof(List<RawRequest>))]
[JsonSerializable(typeof(IReadOnlyList<RawRequest>))]
internal partial class IngestJsonContext : JsonSerializerContext
{
}