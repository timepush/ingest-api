using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;
using Timepush.IngestApi.Configurations;
using Timepush.IngestApi.Errors;

namespace Timepush.IngestApi.Features.Ingest;

public sealed class IngestAuthFilter : IEndpointFilter
{
  private readonly IDatabase _cache;
  private readonly NpgsqlDataSource _db;
  private readonly byte[] _pepper;
  private const string Miss = "__MISS__";

  public IngestAuthFilter(IDatabase cache, NpgsqlDataSource db, IOptions<AppOptions> options)
  {
    _cache = cache;
    _db = db;
    _pepper = Encoding.UTF8.GetBytes(options.Value.Pepper);
  }

  public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
  {
    var h = ctx.HttpContext;
    var ct = h.RequestAborted;

    var auth = h.Request.Headers["Authorization"].ToString();
    var clientId = h.Request.Headers["X-Client-ID"].ToString();
    if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(clientId))
      return Problems.Unauthorized("Missing Authorization or X-Client-ID header.");

    var secret = auth.AsSpan(7).Trim().ToString();
    var cacheKey = $"ds:{clientId}:{HmacHex(_pepper, secret)}";

    var cached = await _cache.StringGetAsync(cacheKey);
    if (!cached.IsNullOrEmpty)
    {
      if (cached == Miss) return Problems.Unauthorized("Invalid client secret or client ID.");
      h.Items["data_source_id"] = (string)cached!;
      return await next(ctx);
    }

    string? dsId = null, hash = null;
    await using (var cmd = _db.CreateCommand("select id::text, client_secret_hash from data_sources where client_id=@cid limit 1"))
    {
      cmd.Parameters.AddWithValue("cid", clientId);
      await using var r = await cmd.ExecuteReaderAsync(ct);
      if (await r.ReadAsync(ct)) { dsId = r.GetString(0); hash = r.IsDBNull(1) ? null : r.GetString(1); }
    }

    if (dsId is null || string.IsNullOrEmpty(hash) || !BCrypt.Net.BCrypt.Verify(secret, hash))
    {
      _ = _cache.StringSetAsync(cacheKey, Miss, TimeSpan.FromMinutes(2));
      return Problems.Unauthorized("Invalid client secret or client ID.");
    }

    var ttl = TimeSpan.FromMinutes(55 + Random.Shared.Next(0, 5));
    _ = _cache.StringSetAsync(cacheKey, dsId, ttl);

    h.Items["data_source_id"] = dsId;
    return await next(ctx);
  }



  private static string HmacHex(byte[] key, string data)
  {
    using var h = new HMACSHA256(key);
    return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
  }


}
