using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Auth;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;

namespace TurkcellMeetingAssistant.UnitTests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private const string AdminPassword = "AdminPassword123!";
    private const string UserPassword = "UserPassword123!";
    private const string AdminEmail = "admin.test@meetingassistant.local";
    private const string UserEmail = "user.test@meetingassistant.local";
    private const string InactiveUserEmail = "inactive.test@meetingassistant.local";
    private const string DeletedUserEmail = "deleted.test@meetingassistant.local";

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!context.Users.Any(u => u.Email == AdminEmail))
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "Test",
                Email = AdminEmail,
                PasswordHash = passwordHasher.HashPassword(AdminPassword),
                Role = UserRole.Admin,
                IsActive = true
            });
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = "User",
                LastName = "Test",
                Email = UserEmail,
                PasswordHash = passwordHasher.HashPassword(UserPassword),
                Role = UserRole.User,
                IsActive = true
            });
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Inactive",
                LastName = "Test",
                Email = InactiveUserEmail,
                PasswordHash = passwordHasher.HashPassword(UserPassword),
                Role = UserRole.User,
                IsActive = false
            });
            
            var deletedUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Deleted",
                LastName = "Test",
                Email = DeletedUserEmail,
                PasswordHash = passwordHasher.HashPassword(UserPassword),
                Role = UserRole.User,
                IsActive = true
            };
            // Note: Our soft delete happens on SaveChanges with EntityState.Deleted
            context.Users.Add(deletedUser);
            context.SaveChanges();
            
            context.Users.Remove(deletedUser);
            context.SaveChanges();
        }
    }

    // 1. Geçerli admin bilgileriyle giriş başarılı.
    [Fact]
    public async Task AdminLogin_WithValidCredentials_ReturnsSuccessAndToken()
    {
        var request = new LoginRequest { Email = AdminEmail, Password = AdminPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    // 2. Geçerli user bilgileriyle giriş başarılı.
    [Fact]
    public async Task UserLogin_WithValidCredentials_ReturnsSuccessAndToken()
    {
        var request = new LoginRequest { Email = UserEmail, Password = UserPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        result!.IsSuccess.Should().BeTrue();
        result.Data!.User.Email.Should().Be(UserEmail);
    }

    // 3. Hatalı parola ile giriş başarısız.
    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorizedAndSameMessage()
    {
        var request = new LoginRequest { Email = UserEmail, Password = "wrongpassword" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        result!.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("E-posta veya şifre hatalı.");
    }

    // 4. Olmayan e-posta ile giriş başarısız.
    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorizedAndSameMessage()
    {
        var request = new LoginRequest { Email = "nonexistent@test.com", Password = UserPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        result!.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("E-posta veya şifre hatalı.");
    }

    // 5. Pasif kullanıcı giriş yapamaz.
    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var request = new LoginRequest { Email = InactiveUserEmail, Password = UserPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        result!.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Kullanıcı hesabı aktif değil.");
    }

    // 6. Login validation hataları doğru döner.
    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        var request = new LoginRequest { Email = "invalid-email", Password = "" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 7. Token olmadan /api/auth/me çağrısı 401 döner.
    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 8. Geçerli token ile /api/auth/me kullanıcıyı döner.
    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserDto()
    {
        var loginReq = new LoginRequest { Email = UserEmail, Password = UserPassword };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.Data!.AccessToken);
        var response = await _client.GetAsync("/api/auth/me");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Email.Should().Be(UserEmail);
        result.Data.Role.Should().Be("User");
    }

    // 9. User rolü admin endpointine erişemez.
    [Fact]
    public async Task AdminEndpoint_AccessedByUserRole_ReturnsForbidden()
    {
        var loginReq = new LoginRequest { Email = UserEmail, Password = UserPassword };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.Data!.AccessToken);
        var response = await _client.GetAsync("/api/auth/admin-test");
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // 10. Admin rolü admin endpointine erişebilir.
    [Fact]
    public async Task AdminEndpoint_AccessedByAdminRole_ReturnsOk()
    {
        var loginReq = new LoginRequest { Email = AdminEmail, Password = AdminPassword };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.Data!.AccessToken);
        var response = await _client.GetAsync("/api/auth/admin-test");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 11. PasswordHash hiçbir API response içinde yer almaz.
    [Fact]
    public async Task LoginResponse_ShouldNotContainPasswordHash()
    {
        var request = new LoginRequest { Email = UserEmail, Password = UserPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        var contentString = await response.Content.ReadAsStringAsync();
        
        contentString.Should().NotContain("PasswordHash");
        contentString.Should().NotContain("passwordHash");
    }

    // 12. Soft delete edilen kullanıcı login olamaz.
    [Fact]
    public async Task Login_WithSoftDeletedUser_ReturnsUnauthorized()
    {
        var request = new LoginRequest { Email = DeletedUserEmail, Password = UserPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        result!.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("E-posta veya şifre hatalı.");
    }

    // 13. Seed işlemi ikinci kez duplicate kullanıcı oluşturmaz.
    [Fact]
    public void SeedProcess_ShouldNotCreateDuplicateUsers()
    {
        // Calling SeedTestData again
        SeedTestData();
        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var adminCount = context.Users.IgnoreQueryFilters().Count(u => u.Email == AdminEmail);
        adminCount.Should().Be(1);
    }

    // 14. Audit log içerisinde parola veya token bulunmaz.
    [Fact]
    public async Task AuditLogs_ShouldNotContainPasswordsOrTokens()
    {
        var request = new LoginRequest { Email = UserEmail, Password = UserPassword };
        await _client.PostAsJsonAsync("/api/auth/login", request); // Generates a login audit
        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var logs = await context.AuditLogs.ToListAsync();
        
        foreach (var log in logs)
        {
            if (log.OldValues != null) log.OldValues.Should().NotContain(UserPassword);
            if (log.NewValues != null) log.NewValues.Should().NotContain(UserPassword);
            if (log.NewValues != null) log.NewValues.Should().NotContain("Bearer");
        }
    }
}
