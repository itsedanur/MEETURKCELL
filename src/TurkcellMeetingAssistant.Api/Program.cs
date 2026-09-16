using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TurkcellMeetingAssistant.Api.Middlewares;
using TurkcellMeetingAssistant.Api.Services;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Infrastructure.Auth;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using TurkcellMeetingAssistant.Infrastructure.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Database
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Configuration Binding
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "")),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings?.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IMeetingParticipantService, MeetingParticipantService>();
builder.Services.Configure<TurkcellMeetingAssistant.Application.Models.AI.AiSettings>(builder.Configuration.GetSection(TurkcellMeetingAssistant.Application.Models.AI.AiSettings.SectionName));

builder.Services.AddScoped<ITranscriptService, TranscriptService>();

// AI Provider Abstraction
builder.Services.AddScoped<TurkcellMeetingAssistant.Infrastructure.Services.AI.AiProviderFactory>();
builder.Services.AddScoped<TurkcellMeetingAssistant.Application.Interfaces.AI.IAiProvider>(sp => 
{
    var factory = sp.GetRequiredService<TurkcellMeetingAssistant.Infrastructure.Services.AI.AiProviderFactory>();
    return factory.CreateProvider();
});
builder.Services.AddScoped<TurkcellMeetingAssistant.Application.Interfaces.AI.IPromptService, TurkcellMeetingAssistant.Infrastructure.Services.AI.FilePromptService>();

// Use the real AiMeetingAnalysisService which depends on IAiProvider
builder.Services.AddScoped<IAiMeetingAnalysisService, AiMeetingAnalysisService>();

// Speech to Text Settings
builder.Services.Configure<TurkcellMeetingAssistant.Application.Models.Speech.SpeechToTextSettings>(builder.Configuration.GetSection(TurkcellMeetingAssistant.Application.Models.Speech.SpeechToTextSettings.SectionName));

// Background Queue
builder.Services.AddSingleton<TurkcellMeetingAssistant.Application.Interfaces.Speech.ITranscriptionQueue, TurkcellMeetingAssistant.Infrastructure.Services.Speech.TranscriptionQueue>();
builder.Services.AddHostedService<TurkcellMeetingAssistant.Infrastructure.Services.Speech.TranscriptionBackgroundService>();

// Speech Storage & Factory
builder.Services.AddScoped<TurkcellMeetingAssistant.Application.Interfaces.Speech.IRecordingStorageService, TurkcellMeetingAssistant.Infrastructure.Services.Speech.LocalRecordingStorageService>();
builder.Services.AddScoped<TurkcellMeetingAssistant.Infrastructure.Services.Speech.AiSpeechProviderFactory>();
builder.Services.AddScoped<TurkcellMeetingAssistant.Application.Interfaces.Speech.ISpeechToTextProvider>(sp => 
{
    var factory = sp.GetRequiredService<TurkcellMeetingAssistant.Infrastructure.Services.Speech.AiSpeechProviderFactory>();
    return factory.CreateProvider();
});
builder.Services.AddScoped<TurkcellMeetingAssistant.Application.Interfaces.Speech.IMeetingRecordingService, TurkcellMeetingAssistant.Infrastructure.Services.Speech.MeetingRecordingService>();

builder.Services.AddScoped<IMeetingAnalysisService, MeetingAnalysisService>();
builder.Services.AddScoped<IMeetingSummaryMutationService, MeetingSummaryMutationService>();
builder.Services.AddScoped<IMeetingSummaryEditorService, MeetingSummaryEditorService>();
builder.Services.AddScoped<IMeetingActionItemService, MeetingActionItemService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IMeetingEmailTemplateService, MeetingEmailTemplateService>();
builder.Services.AddScoped<IMeetingEmailAppService, MeetingEmailAppService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<TurkcellMeetingAssistant.Application.Validators.LoginRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with Bearer Auth
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Turkcell Meeting Assistant API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed Database in Development
    await SeedDatabaseAsync(app);
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Don't auto-migrate on startup for production, but in dev it's ok.
        // Actually, instructions say: "Migration başarılı şekilde oluşturulduktan sonra... migration'ı uygula. PostgreSQL çalışmıyorsa... açıkça belirt."
        // We will just seed if the DB is available and schema is created. 
        // We use context.Database.CanConnectAsync() maybe. Let's just catch exceptions.
        if (await context.Database.CanConnectAsync())
        {
            // Seed Admin
            if (!await context.Users.AnyAsync(u => u.Email == "admin@meetingassistant.local"))
            {
                // Getting passwords from configuration/environment variables
                var adminPassword = app.Configuration["SeedData:AdminPassword"];
                if (string.IsNullOrEmpty(adminPassword))
                {
                    logger.LogWarning("SeedData:AdminPassword is not set. Cannot seed admin user.");
                }
                else
                {
                    context.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "System",
                        LastName = "Admin",
                        Email = "admin@meetingassistant.local",
                        PasswordHash = passwordHasher.HashPassword(adminPassword),
                        Role = UserRole.Admin,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Seed User
            if (!await context.Users.AnyAsync(u => u.Email == "user@meetingassistant.local"))
            {
                var userPassword = app.Configuration["SeedData:UserPassword"];
                if (string.IsNullOrEmpty(userPassword))
                {
                    logger.LogWarning("SeedData:UserPassword is not set. Cannot seed demo user.");
                }
                else
                {
                    context.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Demo",
                        LastName = "User",
                        Email = "user@meetingassistant.local",
                        PasswordHash = passwordHasher.HashPassword(userPassword),
                        Role = UserRole.User,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Seed Sample Meetings if none exist
            if (!await context.Meetings.AnyAsync())
            {
                var demoUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "user@meetingassistant.local");
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@meetingassistant.local");
                var userId = demoUser?.Id ?? Guid.NewGuid();

                var meeting1 = new Meeting
                {
                    Id = Guid.NewGuid(),
                    Title = "Turkcell 5G Altyapı ve AI Asistan Entegrasyonu",
                    Description = "5G şebeke optimizasyonu ve Yapay Zeka destekli toplantı özetleme asistanının canlıya geçiş planlaması.",
                    OrganizerUserId = userId,
                    MeetingDate = DateTime.UtcNow.AddDays(-1).Date,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    Status = MeetingStatus.Approved,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                var meeting2 = new Meeting
                {
                    Id = Guid.NewGuid(),
                    Title = "Mobil & Web Uygulaması Sprint Değerlendirmesi",
                    Description = "Turkcell Meeting Assistant projesinin frontend arayüz geliştirmeleri ve staj defteri dokümantasyonu.",
                    OrganizerUserId = userId,
                    MeetingDate = DateTime.UtcNow.Date,
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    Status = MeetingStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };

                context.Meetings.AddRange(meeting1, meeting2);

                // Add Participants
                var p1 = new MeetingParticipant
                {
                    Id = Guid.NewGuid(),
                    MeetingId = meeting1.Id,
                    FullName = "Edanur Ünal",
                    Email = "user@meetingassistant.local",
                    Department = "Yazılım Geliştirme",
                    Title = "Yazılım Stajyeri",
                    IsOrganizer = true,
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                var p2 = new MeetingParticipant
                {
                    Id = Guid.NewGuid(),
                    MeetingId = meeting1.Id,
                    FullName = "Sistem Yöneticisi",
                    Email = "admin@meetingassistant.local",
                    Department = "Bilgi Teknolojileri",
                    Title = "Kıdemli Mimar",
                    IsOrganizer = false,
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                context.MeetingParticipants.AddRange(p1, p2);

                // Add AI Summary for Meeting 1
                var summary1 = new MeetingSummary
                {
                    Id = Guid.NewGuid(),
                    MeetingId = meeting1.Id,
                    MeetingPurpose = "5G Altyapı optimizasyon stratejisini belirlemek ve AI Asistan entegrasyon durumunu incelemek.",
                    ExecutiveSummary = "Turkcell 5G baz istasyonlarında AI destekli yük dengeleme sistemi başarıyla test edildi. Toplantı asistanı modülü canlı ortama entegre edildi ve otomatik aksiyon maddeleri üretildi.",
                    Version = 1,
                    IsApproved = true,
                    ApprovedAt = DateTime.UtcNow.AddDays(-1),
                    ApprovedByUserId = userId,
                    AiProvider = "MockOpenAI",
                    PromptVersion = "v1.0",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                context.MeetingSummaries.Add(summary1);

                // Add Action Items for Meeting 1
                context.ActionItems.AddRange(
                    new ActionItem
                    {
                        Id = Guid.NewGuid(),
                        MeetingId = meeting1.Id,
                        MeetingSummaryId = summary1.Id,
                        OwnerName = "Edanur Ünal",
                        OwnerEmail = "user@meetingassistant.local",
                        Description = "Staj defteri için projenin canlı sistem ekran görüntülerini derle ve rapora ekle.",
                        DueDate = DateTime.UtcNow.AddDays(1),
                        Status = ActionItemStatus.InProgress,
                        Priority = ActionPriority.High,
                        ConfidenceScore = 0.95m,
                        SourceType = SourceType.AI,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new ActionItem
                    {
                        Id = Guid.NewGuid(),
                        MeetingId = meeting1.Id,
                        MeetingSummaryId = summary1.Id,
                        OwnerName = "Edanur Ünal",
                        OwnerEmail = "user@meetingassistant.local",
                        Description = "Frontend UI temasını Turkcell kurumsal renk paletine (Lacivert #002C5F & Sarı #FFC72C) göre optimize et.",
                        DueDate = DateTime.UtcNow,
                        Status = ActionItemStatus.Completed,
                        CompletedAt = DateTime.UtcNow,
                        Priority = ActionPriority.Medium,
                        ConfidenceScore = 0.98m,
                        SourceType = SourceType.AI,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new ActionItem
                    {
                        Id = Guid.NewGuid(),
                        MeetingId = meeting1.Id,
                        MeetingSummaryId = summary1.Id,
                        OwnerName = "Sistem Yöneticisi",
                        OwnerEmail = "admin@meetingassistant.local",
                        Description = "PostgreSQL veritabanı performansını incele ve indeks tanımlamalarını kontrol et.",
                        DueDate = DateTime.UtcNow.AddDays(3),
                        Status = ActionItemStatus.Open,
                        Priority = ActionPriority.Low,
                        ConfidenceScore = 0.90m,
                        SourceType = SourceType.Manual,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                );
            }

            await context.SaveChangesAsync();
        }
        else
        {
            logger.LogWarning("Cannot connect to database. Seeding skipped.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

public partial class Program { }
