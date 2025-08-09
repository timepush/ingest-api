using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Timepush.IngestApi.Lib;

// Publish batches into a bounded channel
public interface IAsyncBatchPublisher<T>
{
  ValueTask PublishAsync(IReadOnlyList<T> items, CancellationToken ct = default);
}

public sealed class ChannelBatchPublisher<T> : IAsyncBatchPublisher<T>
{
  private readonly ChannelWriter<T> _writer;
  public ChannelBatchPublisher(ChannelWriter<T> writer) => _writer = writer;

  public async ValueTask PublishAsync(IReadOnlyList<T> items, CancellationToken ct = default)
  {
    foreach (var it in items) await _writer.WriteAsync(it, ct);
  }
}

// Background worker: drains channel -> Kafka in timed/size batches
public sealed class KafkaFlushWorker<T> : BackgroundService
{
  private readonly ChannelReader<T> _reader;
  private readonly Func<IReadOnlyList<T>, CancellationToken, Task> _sendBatch;
  private readonly int _maxBatch; private readonly TimeSpan _maxWait;

  public KafkaFlushWorker(ChannelReader<T> reader,
                          Func<IReadOnlyList<T>, CancellationToken, Task> sendBatch,
                          int maxBatch = 1000, int maxWaitMs = 10)
  { _reader = reader; _sendBatch = sendBatch; _maxBatch = maxBatch; _maxWait = TimeSpan.FromMilliseconds(maxWaitMs); }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var buf = new List<T>(_maxBatch);
    var next = DateTime.UtcNow + _maxWait;

    while (!stoppingToken.IsCancellationRequested)
    {
      while (_reader.TryRead(out var item))
      {
        buf.Add(item);
        if (buf.Count >= _maxBatch) { await FlushAsync(buf, stoppingToken); next = DateTime.UtcNow + _maxWait; }
      }

      if (buf.Count > 0 && DateTime.UtcNow >= next)
      { await FlushAsync(buf, stoppingToken); next = DateTime.UtcNow + _maxWait; }

      await _reader.WaitToReadAsync(stoppingToken);
    }
  }

  private Task FlushAsync(List<T> buf, CancellationToken ct)
  {
    var snap = buf.ToArray(); buf.Clear();
    return _sendBatch(snap, ct);
  }
}
