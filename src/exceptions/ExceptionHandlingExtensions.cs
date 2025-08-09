using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Timepush.Ingest.Exceptions;

namespace Timepush.Ingest.Exceptions;

public static class ExceptionHandlingExtensions
{
  public static IServiceCollection AddExceptionHandlers(this IServiceCollection services)
  {
    services.AddProblemDetails(configure =>
    {
      configure.CustomizeProblemDetails = context =>
          {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
            context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
          };
    });
    services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
    services.AddExceptionHandler<GlobalExceptionHandler>();
    return services;
  }
}
