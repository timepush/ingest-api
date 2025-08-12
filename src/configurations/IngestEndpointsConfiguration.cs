using System.Threading.Channels;
using FluentValidation;
using Timepush.IngestApi.Features.Ingest;
using Timepush.IngestApi.Features.Ingest.Raw;

namespace Timepush.IngestApi.Configurations;

public static class IngestEndpointsConfiguration
{
  public static WebApplicationBuilder ConfigureIngestEndpoints(this WebApplicationBuilder builder)
  {
    builder.Services.AddScoped<IValidator<RawRequest>, IngestValidation>();
    builder.Services.AddScoped<IValidator<List<RawRequest>>, BatchIngestValidation>();
    builder.Services.AddScoped<IngestAuthFilter>();
    return builder;
  }
}
