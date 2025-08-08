using Timepush.Ingest.Features.Ingest.Raw;
using Timepush.Ingest.Filters;

public static class IngestEndpoints
{
  public static WebApplication MapIngestEndpoints(this WebApplication app)
  {
    var group = app.MapGroup("/ingest");
    group.MapPost("/raw", RawHandler.IngestData)
      .AddEndpointFilter<ValidationFilter<RawRequest>>()
      .AddEndpointFilter<AuthFilter>();

    group.MapPost("/raw/batch", RawHandler.IngestBatchData)
      .AddEndpointFilter<ValidationFilter<List<RawRequest>>>()
      .AddEndpointFilter<AuthFilter>();


    return app;
  }
}