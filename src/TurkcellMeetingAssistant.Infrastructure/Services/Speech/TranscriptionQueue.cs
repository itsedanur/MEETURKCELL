using System.Threading.Channels;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class TranscriptionQueue : ITranscriptionQueue
{
    private readonly Channel<TranscriptionJob> _queue;

    public TranscriptionQueue()
    {
        // Kapasite eklenebilir, şimdilik Unbounded kullanıyoruz
        var options = new UnboundedChannelOptions
        {
            SingleReader = true, // We only have one background worker reading
            SingleWriter = false
        };
        _queue = Channel.CreateUnbounded<TranscriptionJob>(options);
    }

    public async ValueTask QueueJobAsync(TranscriptionJob job, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<TranscriptionJob> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
