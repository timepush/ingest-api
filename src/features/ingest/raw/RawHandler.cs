using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using Timepush.IngestApi.Lib;

namespace Timepush.IngestApi.Features.Ingest.Raw;

public static class RawHandler
{
  public static async Task<IResult> IngestStream(HttpContext http, IAsyncBatchPublisher<RawRequest> publisher)
  {
    var ct = http.RequestAborted;
    if (http.Items["data_source_id"] is not string dsId)
      return Results.Problem("Missing data_source_id", statusCode: 500);

    await StreamProcessor.ProcessStreamAsync(
      http.Request.BodyReader,
      ct,
      (RawRequest req) => req.Timestamp.Offset == TimeSpan.Zero && !double.IsNaN(req.Value) && !double.IsInfinity(req.Value),
      (RawRequest req) => req.DataSourceId = dsId,
      async batch => await publisher.PublishAsync(batch, ct),
      1000
    );
    return Results.Accepted();
  }
}
