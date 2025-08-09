using Timepush.IngestApi.Features.Ingest.Raw;

namespace Timepush.IngestApi.Features.Ingest;

public static class IngestEndpoints
{
  public static WebApplication MapIngestEndpoints(this WebApplication app)
  {
    var group = app.MapGroup("/ingest");

    group
      .MapPost("/raw/stream", RawHandler.IngestStream)
      .AddEndpointFilter<IngestAuthFilter>();

    // group
    //   .MapPost("/raw/stream", RawHandler.IngestStream);
    // .AddEndpointFilter<IngestAuthFilter>();

    return app;

  }
}