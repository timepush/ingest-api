using System;
using System.Diagnostics;
using Lib.ServerTiming;
using Lib.ServerTiming.Http.Headers;

namespace Timepush.Ingest.Lib;

public static class ServerTimingExtensions
{
  public static async Task<T> TimeAsync<T>(this IServerTiming serverTiming, string metricName, Func<Task<T>> func, string? description = null)
  {
    var sw = Stopwatch.StartNew();
    T result = await func();
    sw.Stop();
    serverTiming?.Metrics.Add(new ServerTimingMetric(metricName, (decimal)sw.Elapsed.TotalMilliseconds, description));
    return result;
  }

  public static async Task TimeAsync(this IServerTiming serverTiming, string metricName, Func<Task> func, string? description = null)
  {
    var sw = Stopwatch.StartNew();
    await func();
    sw.Stop();
    serverTiming?.Metrics.Add(new ServerTimingMetric(metricName, (decimal)sw.Elapsed.TotalMilliseconds, description));
  }
}