using TurkcellMeetingAssistant.Application.Common.Models;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
