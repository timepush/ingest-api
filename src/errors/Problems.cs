namespace Timepush.IngestApi.Errors;

public static class Problems
{
  public static IResult Unauthorized(string? detail = null) =>
    Results.Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized, detail: detail);

  public static IResult NotFound(string? detail = null) =>
    Results.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, detail: detail);

  public static IResult ValidationFailed(string? detail = null) =>
    Results.Problem(title: "Validation Failed", statusCode: StatusCodes.Status422UnprocessableEntity, detail: detail);

  public static IResult InternalServerError(string? detail = null) =>
    Results.Problem(title: "Internal Server Error", statusCode: StatusCodes.Status500InternalServerError, detail: detail);
}
