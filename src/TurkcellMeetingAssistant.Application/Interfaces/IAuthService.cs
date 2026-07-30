using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Auth;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<UserDto>> GetMeAsync(CancellationToken cancellationToken);
}
