using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;

namespace Timepush.Ingest.Features.Ingest.Raw;

public class Validation : AbstractValidator<RawRequest>
{
  public Validation()
  {
    RuleFor(x => x.Timestamp)
        .Must(t => t.Offset == TimeSpan.Zero) // Require UTC
        .WithMessage("Timestamp must be in UTC (ending with Z).");
    RuleFor(x => x.Value)
            .Must(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .WithMessage("Value must be a finite number.");

    RuleFor(x => x.Metadata).Custom((m, ctx) =>
        {
          if (m is null) return;

          if (m is not JsonObject)
          {
            ctx.AddFailure("metadata", "metadata must be a JSON object (not array/primitive).");
            return;
          }

          // Explicitly pass the optional parameter as null to avoid CS0854.
          var json = JsonSerializer.Serialize(m, (JsonSerializerOptions?)null);

          // If you care about transport size, measure bytes, not chars:
          if (Encoding.UTF8.GetByteCount(json) > 32_768)
            ctx.AddFailure("metadata", $"metadata is too large (max {32_768} bytes).");
        });
  }
}

public class BatchValidation : AbstractValidator<List<RawRequest>>
{
  public BatchValidation()
  {
    RuleFor(x => x)
      .NotEmpty()
      .WithMessage("Batch cannot be empty.");
    RuleForEach(x => x).SetValidator(new Validation());
  }
}

