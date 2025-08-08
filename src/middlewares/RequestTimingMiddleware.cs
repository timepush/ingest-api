using System.Diagnostics;
using Lib.ServerTiming;
using Lib.ServerTiming.Http.Headers;

namespace Timepush.Ingest.Middlewares;

public class RequestTimingMiddleware
{
  private readonly RequestDelegate _next;

  public RequestTimingMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context, IServerTiming serverTiming)
  {
    var sw = Stopwatch.StartNew();
    await _next(context);
    sw.Stop();

    serverTiming?.Metrics.Add(new ServerTimingMetric("total", (decimal)sw.Elapsed.TotalMilliseconds));
  }
}
