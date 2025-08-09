using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Timepush.IngestApi.Configurations;


public sealed record AppOptions
{
  [Required]
  [ConfigurationKeyName("POSTGRES")]
  public required string Postgres { get; init; }

  [Required]
  [ConfigurationKeyName("REDIS")]
  public required string Redis { get; init; }

  [Required]
  [ConfigurationKeyName("KAFKA_BROKERS")]
  public required string Brokers { get; init; }

  [Required]
  [ConfigurationKeyName("KAFKA_TOPIC_NAME")]
  public required string Topic { get; init; }

  [Range(1, 10_000)]
  [ConfigurationKeyName("KAFKA_LINGER_MS")]
  public int? LingerMs { get; init; } = 10;

  [Required]
  [ConfigurationKeyName("AUTH_PEPPER")]
  public required string Pepper { get; init; }
}

public static class OptionsConfiguration
{
  public static WebApplicationBuilder ConfigureOptions(this WebApplicationBuilder builder)
  {
    builder.Services
       .AddOptions<AppOptions>()
       .Bind(builder.Configuration)
       .ValidateDataAnnotations()
       .ValidateOnStart();

    return builder;
  }

  public static AppOptions GetOptions(this IServiceProvider services)
  {
    var options = services.GetRequiredService<IOptions<AppOptions>>().Value;
    if (options is null)
      throw new InvalidOperationException($"Options type {typeof(AppOptions).Name} not registered");
    return options;
  }

  public static T GetOption<T>(this ConfigurationManager configuration, string keyOrSection)
  {
    // For scalar values (string, int, bool, etc.)
    if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal))
    {
      var value = configuration[keyOrSection];
      if (value == null)
        throw new OptionsValidationException(typeof(T).Name, typeof(T), new[] { $"Missing configuration key '{keyOrSection}'" });

      return (T)Convert.ChangeType(value, typeof(T))!;
    }

    // For complex objects bound from a section
    var section = configuration.GetSection(keyOrSection);
    var instance = section.Get<T>();

    if (instance == null)
      throw new OptionsValidationException(typeof(T).Name, typeof(T), new[] { $"Binding returned null for section '{keyOrSection}'" });

    // DataAnnotations validation for complex types
    Validator.ValidateObject(instance!, new ValidationContext(instance!), validateAllProperties: true);

    return instance;
  }

}
