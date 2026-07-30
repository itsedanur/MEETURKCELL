using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TurkcellMeetingAssistant.Api.Controllers;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Enums;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Controllers;

public class MeetingsControllerTests
{
    private readonly Mock<IMeetingService> _mockMeetingService;
    private readonly Mock<IMeetingParticipantService> _mockParticipantService;
    private readonly Mock<ITranscriptService> _mockTranscriptService;
    private readonly Mock<IMeetingAnalysisService> _mockAnalysisService;
    private readonly Mock<IMeetingSummaryEditorService> _mockSummaryEditorService;
    private readonly Mock<IMeetingActionItemService> _mockActionItemService;
    private readonly MeetingsController _sut;

    public MeetingsControllerTests()
    {
        _mockMeetingService = new Mock<IMeetingService>();
        _mockParticipantService = new Mock<IMeetingParticipantService>();
        _mockTranscriptService = new Mock<ITranscriptService>();
        _mockAnalysisService = new Mock<IMeetingAnalysisService>();
        _mockSummaryEditorService = new Mock<IMeetingSummaryEditorService>();
        _mockActionItemService = new Mock<IMeetingActionItemService>();

        _sut = new MeetingsController(
            _mockMeetingService.Object,
            _mockParticipantService.Object,
            _mockTranscriptService.Object,
            _mockAnalysisService.Object,
            _mockSummaryEditorService.Object,
            _mockActionItemService.Object);
    }

    [Fact]
    public async Task CreateMeeting_ShouldReturnCreatedAtAction_WhenValidRequest()
    {
        // Arrange
        var request = new CreateMeetingRequest { Title = "Test" };
        var expectedId = Guid.NewGuid();
        _mockMeetingService.Setup(x => x.CreateMeetingAsync(request, default)).ReturnsAsync(expectedId);

        // Act
        var result = await _sut.CreateMeeting(request, default);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<Guid>>().Subject;
        apiResponse.Data.Should().Be(expectedId);
        apiResponse.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetMeetings_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var pagedResult = new PagedResult<MeetingListItemDto> { TotalCount = 1 };
        _mockMeetingService.Setup(x => x.GetMeetingsAsync(1, 10, null, null, null, null, false, "MeetingDate", "desc", default)).ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetMeetings(1, 10, null, null, null, null, false, "MeetingDate", "desc", default);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<MeetingListItemDto>>>().Subject;
        apiResponse.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task UploadTranscriptFile_ShouldReturnOk_WhenValidFile()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        var content = "Hello World";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(x => x.OpenReadStream()).Returns(ms);
        fileMock.Setup(x => x.FileName).Returns("test.txt");
        fileMock.Setup(x => x.ContentType).Returns("text/plain");
        fileMock.Setup(x => x.Length).Returns(ms.Length);

        // Act
        var result = await _sut.UploadTranscriptFile(meetingId, fileMock.Object, default);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.IsSuccess.Should().BeTrue();
        
        _mockTranscriptService.Verify(x => x.ProcessTranscriptFileAsync(meetingId, It.IsAny<Stream>(), "test.txt", "text/plain", default), Times.Once);
    }
    #region Email Endpoints

    [Fact]
    public async Task PreviewEmail_ShouldReturnOk_WithEmailPreviewDto()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var request = new TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.GenerateEmailPreviewRequest();
        var responseDto = new TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailPreviewDto { Subject = "Subject", HtmlBody = "Body" };

        var mockAppService = new Mock<IMeetingEmailAppService>();
        mockAppService.Setup(s => s.PreviewEmailAsync(meetingId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddScoped(_ => mockAppService.Object)
            .BuildServiceProvider();

        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };

        // Act
        var result = await _sut.PreviewEmail(meetingId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeOfType<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailPreviewDto>().Subject;
        returnValue.Subject.Should().Be("Subject");
    }

    [Fact]
    public async Task SendMeetingEmail_ShouldReturnOk_WithEmailLogDto()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var request = new TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.SendMeetingEmailRequest();
        var responseDto = new TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto { Id = Guid.NewGuid(), Status = TurkcellMeetingAssistant.Domain.Enums.EmailDeliveryStatus.Sent };

        var mockAppService = new Mock<IMeetingEmailAppService>();
        mockAppService.Setup(s => s.SendMeetingEmailAsync(meetingId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddScoped(_ => mockAppService.Object)
            .BuildServiceProvider();

        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };

        // Act
        var result = await _sut.SendMeetingEmail(meetingId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeOfType<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto>().Subject;
        returnValue.Status.Should().Be(TurkcellMeetingAssistant.Domain.Enums.EmailDeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendTestEmail_ShouldReturnOk_WithEmailLogDto()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var request = new TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.SendTestEmailRequest { ToEmail = "test@test.com" };
        var responseDto = new TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto { Id = Guid.NewGuid(), Status = TurkcellMeetingAssistant.Domain.Enums.EmailDeliveryStatus.Sent };

        var mockAppService = new Mock<IMeetingEmailAppService>();
        mockAppService.Setup(s => s.SendTestEmailAsync(meetingId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddScoped(_ => mockAppService.Object)
            .BuildServiceProvider();

        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };

        // Act
        var result = await _sut.SendTestEmail(meetingId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeOfType<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto>().Subject;
        returnValue.Status.Should().Be(TurkcellMeetingAssistant.Domain.Enums.EmailDeliveryStatus.Sent);
    }

    #endregion
}
