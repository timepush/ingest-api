using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Timepush.IngestApi.Errors;

namespace Timepush.IngestApi.Lib;

public static class NotFoundExtensions
{
  public static IApplicationBuilder MapNotFound(this IApplicationBuilder app)
  {
    return app.UseMiddleware<NotFoundMiddleware>();
  }
}

public class NotFoundMiddleware
{
  private readonly RequestDelegate _next;

  public NotFoundMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    await _next(context);
    if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
    {
      context.Response.ContentType = "application/problem+json";
      var result = Problems.NotFound();
      await result.ExecuteAsync(context);
    }
  }
}
