using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class TranscriptionBackgroundService : BackgroundService
{
    private readonly ITranscriptionQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TranscriptionBackgroundService> _logger;

    public TranscriptionBackgroundService(ITranscriptionQueue queue, IServiceProvider serviceProvider, ILogger<TranscriptionBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transcription Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                
                _logger.LogInformation("Processing transcription job for RecordingId: {RecordingId}", job.RecordingId);

                using var scope = _serviceProvider.CreateScope();
                var recordingService = scope.ServiceProvider.GetRequiredService<IMeetingRecordingService>();
                
                await recordingService.ProcessRecordingAsync(job.RecordingId, stoppingToken);
                
                _logger.LogInformation("Completed transcription job for RecordingId: {RecordingId}", job.RecordingId);
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing transcription job.");
            }
        }

        _logger.LogInformation("Transcription Background Service is stopping.");
    }
}
