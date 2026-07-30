using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
