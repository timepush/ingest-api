using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace Timepush.IngestApi.Lib;

public static class StreamProcessor
{
  public static async Task ProcessStreamAsync<TModel>(
      PipeReader reader,
      CancellationToken ct,
      Func<TModel, bool> validate,
      Action<TModel> postProcess,
      Func<List<TModel>, Task> publishBatch,
      int batchSize = 1000)
  {
    var batch = new List<TModel>(batchSize);

    while (true)
    {
      var read = await reader.ReadAsync(ct);
      var buffer = read.Buffer;

      // Pull full lines using the sync helper (uses SequenceReader internally)
      while (TryReadLine(ref buffer, out var line))
      {
        if (EndsWithCR(line)) line = line.Slice(0, line.Length - 1);

        if (!line.IsEmpty && TryParse(line, out TModel? item) && validate(item!))
        {
          postProcess(item!);
          batch.Add(item!);
          if (batch.Count >= batchSize)
          {
            await publishBatch(batch);
            batch.Clear();
          }
        }
      }

      // Final leftover without LF on completed request: treat as a line
      if (read.IsCompleted && buffer.Length > 0)
      {
        var line = buffer;
        if (EndsWithCR(line)) line = line.Slice(0, line.Length - 1);

        if (!line.IsEmpty && TryParse(line, out TModel? item) && validate(item!))
        {
          postProcess(item!);
          batch.Add(item!);
        }
        buffer = buffer.Slice(buffer.End);
      }

      reader.AdvanceTo(buffer.Start, buffer.End);
      if (read.IsCompleted) break;
    }

    if (batch.Count > 0) await publishBatch(batch);
  }

  // ---- sync helpers (ok to use ref-structs here) ----

  private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
  {
    var sr = new SequenceReader<byte>(buffer);
    if (sr.TryReadTo(out line, (byte)'\n', advancePastDelimiter: true))
    {
      buffer = buffer.Slice(sr.Position); // commit consumed bytes
      return true;
    }
    line = default;
    return false;
  }

  private static bool EndsWithCR(in ReadOnlySequence<byte> seq)
  {
    if (seq.IsEmpty) return false;
    var tail = seq.Slice(seq.Length - 1, 1);
    return tail.FirstSpan.Length > 0 && tail.FirstSpan[0] == (byte)'\r';
  }

  private static bool TryParse<T>(in ReadOnlySequence<byte> line, out T? item)
  {
    try
    {
      if (line.IsSingleSegment)
      {
        item = JsonSerializer.Deserialize<T>(line.FirstSpan, JsonDefaults.SerializerOptions);
        return item is not null;
      }
      var arr = line.ToArray(); // rare: multi-segment
      item = JsonSerializer.Deserialize<T>(arr, JsonDefaults.SerializerOptions);
      return item is not null;
    }
    catch (JsonException e)
    {
      item = default;
      return false;
    }
  }
}
