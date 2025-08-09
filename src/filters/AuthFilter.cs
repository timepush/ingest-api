using Dapper;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using StackExchange.Redis;
using Timepush.Ingest.Exceptions;
using System.Diagnostics;
using Lib.ServerTiming;
using Lib.ServerTiming.Http.Headers;
using Timepush.Ingest.Lib;

namespace Timepush.Ingest.Filters;

public class AuthFilter : IEndpointFilter
{
  private readonly IDatabase _redis;
  private readonly NpgsqlDataSource _dataSource;
  private readonly ILogger<AuthFilter> _logger;
  private readonly IServerTiming _serverTiming;

  public AuthFilter(IConnectionMultiplexer redis, NpgsqlDataSource dataSource, ILogger<AuthFilter> logger, IServerTiming serverTiming)
  {
    _redis = redis.GetDatabase();
    _dataSource = dataSource;
    _logger = logger;
    _serverTiming = serverTiming;
  }

  public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
  {
    var http = context.HttpContext;

    // Extract headers
    var authHeader = http.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
    var clientId = http.Request.Headers["X-Client-ID"].FirstOrDefault();

    if (!authHeader.StartsWith("Bearer ") || string.IsNullOrEmpty(clientId))
    {
      _logger.LogDebug("Missing Authorization or X-Client-ID header");
      return Results.Problem(
        title: "Unauthorized",
        detail: "Missing Authorization or X-Client-ID header",
        statusCode: StatusCodes.Status401Unauthorized
      );
    }

    var rawSecret = authHeader["Bearer ".Length..].Trim();

    using var sha = SHA256.Create();
    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(rawSecret));
    string fp = Convert.ToHexString(hash).ToLowerInvariant();

    var cacheKey = $"ds:client:{clientId}:fp:{fp}";
    string? dataSourceId = null;


    var cached = await _serverTiming.TimeAsync("redis_get", () => _redis.StringGetAsync(cacheKey));

    if (!cached.IsNullOrEmpty)
    {
      dataSourceId = cached;
      _logger.LogInformation("Cache hit for clientId={ClientId}", clientId);
    }
    else
    {

      _logger.LogInformation("Cache miss for clientId={ClientId}, querying Postgres", clientId);
      await using var conn = await _serverTiming.TimeAsync("postgres_open", () => _dataSource.OpenConnectionAsync().AsTask());

      var result = await _serverTiming.TimeAsync("postgres", () =>
      {
        var sql = "SELECT id, client_secret_hash FROM data_sources WHERE client_id = @clientId";
        return conn.QueryFirstOrDefaultAsync<(object id, string client_secret_hash)>(sql, new { clientId });
      });

      if (result.id == null)
      {
        _logger.LogDebug("Invalid client id: {ClientId}", clientId);
        return Results.Problem(
          title: "Unauthorized",
          detail: $"Invalid client id for client id={clientId}",
          statusCode: StatusCodes.Status401Unauthorized
        );
      }

      var clientSecretHash = result.client_secret_hash;

      // bcrypt compare
      var ok = BCrypt.Net.BCrypt.Verify(rawSecret, clientSecretHash);
      if (!ok)
      {
        _logger.LogDebug("Invalid client secret for client id={ClientId}", clientId);
        return Results.Problem(
          title: "Unauthorized",
          detail: $"Invalid client secret for client id={clientId}",
          statusCode: StatusCodes.Status401Unauthorized
        );
      }

      dataSourceId = result.id.ToString();

      await _serverTiming.TimeAsync("redis_set", () => _redis.StringSetAsync(cacheKey, dataSourceId, TimeSpan.FromSeconds(3600)));
    }

    http.Items["data_source_id"] = dataSourceId;

    return await next(context);
  }
}
