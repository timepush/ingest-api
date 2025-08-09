using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Timepush.Ingest.Lib;

public class AppSettings
{
  [Required]
  [ConfigurationKeyName("POSTGRES")]
  public string Postgres { get; set; }

  [Required]
  [ConfigurationKeyName("REDIS")]
  public string Redis { get; set; }

  [Required]
  [ConfigurationKeyName("KAFKA_BROKERS")]
  public string Brokers { get; set; }

  [Required]
  [ConfigurationKeyName("KAFKA_TOPIC_NAME")]
  public string Topic { get; set; }
}

public static class App
{
  public static WebApplicationBuilder ConfigureApp(this WebApplicationBuilder builder)
  {
    builder.Services
       .AddOptions<AppSettings>()
       .Bind(builder.Configuration)
       .ValidateDataAnnotations()
       .ValidateOnStart();


    builder.Services.ConfigureHttpJsonOptions(opts =>
    {
      opts.SerializerOptions.PropertyNameCaseInsensitive = true;
      opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

    return builder;
  }
}