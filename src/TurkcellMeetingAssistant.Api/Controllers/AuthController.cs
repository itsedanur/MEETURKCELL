using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Auth;
using TurkcellMeetingAssistant.Application.Interfaces;

namespace TurkcellMeetingAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken cancellationToken)
    {
        var result = await _authService.GetMeAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    // Temporary endpoint for testing role-based authorization
    [HttpGet("admin-test")]
    [Authorize(Roles = "Admin")]
    public ActionResult<ApiResponse<string>> AdminTest()
    {
        return Ok(ApiResponse<string>.Success("Admin rolüne sahip olduğunuz için bu endpoint'e erişebilirsiniz.", "Başarılı"));
    }
}
