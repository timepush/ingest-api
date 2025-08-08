using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Timepush.Ingest.Lib;

public static class Postgres
{
  public static async Task WarmupAsync(NpgsqlDataSource dataSource, int connections = 5)
  {
    for (int i = 0; i < connections; i++)
    {
      await using var conn = await dataSource.OpenConnectionAsync();
      await conn.ExecuteAsync("SELECT 1 from data_sources LIMIT 1");
    }
  }

  public static IServiceCollection AddPostgres(this IServiceCollection services)
  {
    services.AddSingleton<NpgsqlDataSource>(sp =>
    {
      var appSettings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
      var dataSource = NpgsqlDataSource.Create(appSettings.Postgres);

      _ = WarmupAsync(dataSource, 2);
      return dataSource;
    });
    return services;
  }
}