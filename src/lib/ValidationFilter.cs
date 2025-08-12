using FluentValidation;

namespace Timepush.IngestApi.Lib;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
  public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
  {
    var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
    if (validator != null)
    {
      if (context.Arguments.FirstOrDefault(a => a is T) is T model)
      {
        var result = await validator.ValidateAsync(model);
        if (!result.IsValid)
        {
          var firstError = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
          return Results.Problem(
            title: "Validation Error",
            detail: firstError,
            statusCode: StatusCodes.Status400BadRequest
          );
        }
      }
    }
    return await next(context);
  }
}