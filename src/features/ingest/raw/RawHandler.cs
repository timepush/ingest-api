using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using Timepush.IngestApi.Configurations;
using Timepush.IngestApi.Lib;

namespace Timepush.IngestApi.Features.Ingest.Raw;

public static class RawHandler
{
  public static async Task<IResult> IngestBatchData(HttpContext http, List<RawRequest> requests, KafkaMessageSender sender)
  {
    if (http.Items["data_source_id"] is not string dsId)
    {
      return Results.Problem(
        title: "Internal Error",
        detail: "Could not find data source ID in request context.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    var batch = requests.Select(r => r with { DataSourceId = dsId }).ToList();
    await sender.SendBatchMessageAsync(batch);

    Console.WriteLine($"Received batch: {batch.Count} items");
    return Results.Accepted();
  }

  public static async Task<IResult> IngestData(HttpContext http, RawRequest req, KafkaMessageSender sender)
  {
    if (http.Items["data_source_id"] is not string dsId)
    {
      return Results.Problem(
        title: "Internal Error",
        detail: "Could not find data source ID in request context.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    req = req with { DataSourceId = dsId };
    await sender.SendMessageAsync(req);

    Console.WriteLine($"Received data: {JsonSerializer.Serialize(req, JsonDefaults.SerializerOptions)}");
    return Results.Accepted();
  }
}
