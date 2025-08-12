
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;
using System;
using System.Collections.Generic;
using Timepush.IngestApi.Features.Ingest.Raw;

namespace Timepush.IngestApi.Features.Ingest.Raw;

public class IngestValidation : AbstractValidator<RawRequest>
{
  public IngestValidation()
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

public class BatchIngestValidation : AbstractValidator<List<RawRequest>>
{
  public BatchIngestValidation()
  {
    RuleFor(x => x)
      .NotEmpty()
      .WithMessage("Batch cannot be empty.")
      .Must(x => x.Count <= 86400)
      .WithMessage("Batch cannot exceed 86,400 rows.");
    RuleForEach(x => x).SetValidator(new IngestValidation());
  }
}

