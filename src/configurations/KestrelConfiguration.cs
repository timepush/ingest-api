namespace Timepush.IngestApi.Configurations;

public static class KestrelConfiguration
{
  public static WebApplicationBuilder ConfigureKestrelServer(this WebApplicationBuilder builder)
  {
    builder.WebHost.ConfigureKestrel(o =>
    {
      o.AddServerHeader = false;
      o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
      o.Limits.Http2.MaxStreamsPerConnection = 1000;
      o.Limits.Http2.InitialConnectionWindowSize = 2 * 1024 * 1024; // 2 MB
      o.Limits.Http2.InitialStreamWindowSize = 2 * 1024 * 1024; // 2 MB
      o.AllowSynchronousIO = false;
      o.ListenAnyIP(5000);
    });
    ThreadPool.SetMinThreads(workerThreads: Environment.ProcessorCount * 4, completionPortThreads: Environment.ProcessorCount * 4);
    return builder;
  }
}
