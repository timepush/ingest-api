using Npgsql;

namespace Timepush.IngestApi.Configurations;


public static class PostgresConfiguration
{
  public static WebApplicationBuilder ConfigurePostgres(this WebApplicationBuilder builder)
  {
    builder.Services.AddSingleton(sp =>
      {
        var options = sp.GetOptions();
        var builder = new NpgsqlDataSourceBuilder(options.Postgres);
        return builder.Build();
      });
    return builder;
  }


}