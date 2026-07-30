namespace TurkcellMeetingAssistant.Application.Models.Speech;

public class SpeechToTextSettings
{
    public const string SectionName = "SpeechToText";

    public string Provider { get; set; } = "Mock"; // "Mock", "OpenAI"
    public long MaxFileSizeMb { get; set; } = 25; // Default OpenAI limit
    public string[] AllowedExtensions { get; set; } = new[] { ".mp3", ".wav", ".m4a", ".mp4", ".webm", ".ogg" };
    public string DefaultLanguage { get; set; } = "tr";
    public bool EnableChunking { get; set; } = false; // Phase 9 might not implement chunking yet
}
