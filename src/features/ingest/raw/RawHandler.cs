

using KafkaFlow.Producers;
using Lib.ServerTiming;
using Timepush.Ingest.Lib;

namespace Timepush.Ingest.Features.Ingest.Raw;

public static class RawHandler
{
  public static async Task<IResult> IngestData(HttpContext http, RawRequest req, KafkaMessageSender sender)
  {
    if (http.Items.TryGetValue("data_source_id", out var dsId) && dsId is string datasourceId)
    {
      req = req with { DataSourceId = datasourceId };
    }
    else
    {
      return Results.Problem(
        title: "Internal Error",
        detail: "Could not find data source ID in request context.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    await sender.SendMessageAsync(req);
    return Results.Accepted();
  }

  public static async Task<IResult> IngestBatchData(HttpContext http, List<RawRequest> requests, KafkaMessageSender sender)
  {
    if (http.Items.TryGetValue("data_source_id", out var dsId) && dsId is string datasourceId)
    {
      requests = requests.Select(req => req with { DataSourceId = datasourceId }).ToList();
    }
    else
    {
      return Results.Problem(
        title: "Internal Error",
        detail: "Could not find data source ID in request context.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    await sender.SendBatchMessageAsync(requests);
    return Results.Accepted();
  }
}