using Timepush.IngestApi.Features.Ingest.Raw;
using Timepush.IngestApi.Lib;

namespace Timepush.IngestApi.Features.Ingest;

public static class IngestEndpoints
{
  public static WebApplication MapIngestEndpoints(this WebApplication app)
  {
    var group = app.MapGroup("/ingest");

    group
      .MapPost("/raw", RawHandler.IngestData)
      .AddEndpointFilter<ValidationFilter<RawRequest>>()
      .AddEndpointFilter<IngestAuthFilter>();

    group
      .MapPost("/raw/batch", RawHandler.IngestBatchData)
      .AddEndpointFilter<ValidationFilter<List<RawRequest>>>()
      .AddEndpointFilter<IngestAuthFilter>();

    return app;
  }
}