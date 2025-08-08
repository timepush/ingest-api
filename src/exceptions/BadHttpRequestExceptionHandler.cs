using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Timepush.Ingest.Exceptions;

public class BadHttpRequestExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<BadHttpRequestExceptionHandler> logger) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
  {
    if (exception is not BadHttpRequestException badHttpRequestException)
    {
      return false;
    }

    logger.LogError(exception, "Bad HTTP request error occurred");

    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    var context = new ProblemDetailsContext
    {
      HttpContext = httpContext,
      Exception = exception,
      ProblemDetails = new ProblemDetails
      {
        Type = "Bad Request",
        Title = "Bad HTTP request error occurred",
        Detail = exception.Message,
        Status = StatusCodes.Status400BadRequest
      }
    };


    return await problemDetailsService.TryWriteAsync(context);
  }
}