using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Timepush.Ingest.Lib;


public static class Redis
{
  public static IServiceCollection AddRedis(this IServiceCollection services)
  {
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
      var appSettings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
      var redis = ConnectionMultiplexer.Connect(appSettings.Redis);
      return redis;
    });
    return services;
  }
}