using Timepush.IngestApi.Lib;

namespace Timepush.IngestApi.Configurations;

public static class JsonConfiguration
{
  public static WebApplicationBuilder ConfigureJson(this WebApplicationBuilder builder)
  {
    builder.Services.ConfigureHttpJsonOptions(o =>
    {
      o.SerializerOptions.Converters.Insert(0, new UtcDateTimeOffsetConverter());
      o.SerializerOptions.TypeInfoResolverChain.Insert(0, IngestJsonContext.Default);
    });
    return builder;
  }


}