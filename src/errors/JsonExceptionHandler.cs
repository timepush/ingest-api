using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Timepush.IngestApi.Errors;

public class JsonExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<JsonExceptionHandler> logger) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
  {
    var jsonEx = exception as JsonException ?? (exception as BadHttpRequestException)?.InnerException as JsonException;

    if (jsonEx is null) return false;

    logger.LogError(exception, "Invalid JSON error occurred");

    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    var context = new ProblemDetailsContext
    {
      HttpContext = httpContext,
      Exception = jsonEx,
      ProblemDetails = new ProblemDetails
      {
        Type = "Bad Request",
        Title = "Invalid JSON",
        Detail = jsonEx.Message,
        Status = StatusCodes.Status400BadRequest
      }
    };


    return await problemDetailsService.TryWriteAsync(context);
  }
}