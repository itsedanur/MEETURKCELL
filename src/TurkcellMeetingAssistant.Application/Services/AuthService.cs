using Microsoft.EntityFrameworkCore;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Auth;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;

    // Use DbContext directly to avoid generic repository overhead as specified
    public AuthService(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, ICurrentUserService currentUserService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Log failed attempt here (without password)
            await LogAuditAsync(user?.Id, "User", user?.Id.ToString(), "LoginFailed", null, "Failed login attempt");
            return ApiResponse<LoginResponse>.Fail("E-posta veya şifre hatalı.");
        }

        if (!user.IsActive)
        {
            await LogAuditAsync(user.Id, "User", user.Id.ToString(), "LoginFailed", null, "Inactive user login attempt");
            return ApiResponse<LoginResponse>.Fail("Kullanıcı hesabı aktif değil.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await LogAuditAsync(user.Id, "User", user.Id.ToString(), "LoginSuccess", null, "Successful login");

        var token = _jwtTokenService.GenerateToken(user);
        
        // Expiration is managed in JwtSettings, here we just return a simple calculation for UI
        // We'll leave ExpiresAt roughly to the token expiration
        // For precision we could decode token or get it from settings
        var response = new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Hardcoded for DTO as simple representation, better would be reading from config
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt
            }
        };

        return ApiResponse<LoginResponse>.Success(response);
    }

    public async Task<ApiResponse<UserDto>> GetMeAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
        {
            return ApiResponse<UserDto>.Fail("Kullanıcı doğrulanamadı.");
        }

        var user = await _context.Users.FindAsync(new object[] { _currentUserService.UserId.Value }, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı veya pasif.");
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        };

        return ApiResponse<UserDto>.Success(userDto);
    }

    private async Task LogAuditAsync(Guid? userId, string entityName, string? entityId, string action, string? oldValues, string? newValues)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(default);
    }
}
