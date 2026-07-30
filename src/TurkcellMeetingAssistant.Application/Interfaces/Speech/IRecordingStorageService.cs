namespace TurkcellMeetingAssistant.Application.Interfaces.Speech;

public interface IRecordingStorageService
{
    Task<string> SaveRecordingAsync(Stream fileStream, string originalFileName, CancellationToken cancellationToken = default);
    Task DeleteRecordingAsync(string storageKey, CancellationToken cancellationToken = default);
    string GetFullPath(string storageKey);
}
