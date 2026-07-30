using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class LocalRecordingStorageService : IRecordingStorageService
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LocalRecordingStorageService> _logger;
    private readonly string _uploadDirectory;

    public LocalRecordingStorageService(IHostEnvironment environment, ILogger<LocalRecordingStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        
        // C:\...\Uploads\Recordings (veya linux'ta muadili)
        _uploadDirectory = Path.Combine(_environment.ContentRootPath, "Uploads", "Recordings");
        
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<string> SaveRecordingAsync(Stream fileStream, string originalFileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        // Generate a random, safe storage key to prevent Path Traversal
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        
        var fullPath = GetFullPath(storageKey);

        _logger.LogInformation("Saving recording to {FullPath}", fullPath);

        using var fileStreamToWrite = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(fileStreamToWrite, cancellationToken);
        
        return storageKey;
    }

    public Task DeleteRecordingAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted recording file {FullPath}", fullPath);
        }
        return Task.CompletedTask;
    }

    public string GetFullPath(string storageKey)
    {
        // Prevent path traversal
        var safeKey = Path.GetFileName(storageKey); 
        return Path.Combine(_uploadDirectory, safeKey);
    }
}
