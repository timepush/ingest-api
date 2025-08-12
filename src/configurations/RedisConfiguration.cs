

using StackExchange.Redis;

namespace Timepush.IngestApi.Configurations;

public static class RedisConfiguration
{
  public static WebApplicationBuilder ConfigureRedis(this WebApplicationBuilder builder)
  {
    var redis = builder.Configuration.GetOption<string>("REDIS");
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));
    builder.Services.AddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

    return builder;
  }
}
