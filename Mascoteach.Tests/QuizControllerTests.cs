using System.Security.Claims;
using Mascoteach.API.Controllers;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class QuizControllerTests
{
    private readonly Mock<IQuizService> _service = new();
    private readonly QuizController _sut;

    public QuizControllerTests()
    {
        _sut = new QuizController(_service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("UserId", "10"),
                        new Claim(ClaimTypes.Role, "Teacher")
                    ], "TestAuth"))
                }
            }
        };
    }

    [Fact]
    public async Task Publish_ValidRequest_UsesCurrentUserAndReturnsOk()
    {
        var request = new QuizPublishRequest
        {
            DocumentId = 1,
            Title = "Cards",
            ActivityType = "Flashcard",
            Questions =
            [
                new QuizPublishQuestionRequest
                {
                    QuestionText = "Front",
                    QuestionType = "Flashcard",
                    Position = 0,
                    Options = [new QuizPublishOptionRequest { OptionText = "Back", IsCorrect = true }]
                }
            ]
        };
        var response = new QuizDetailResponse { Id = 5, Title = "Cards", ActivityType = "Flashcard" };
        _service.Setup(service => service.PublishAsync(10, request)).ReturnsAsync(response);

        var result = await _sut.Publish(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        _service.Verify(service => service.PublishAsync(10, request), Times.Once);
    }

    [Fact]
    public async Task Publish_InvalidRequest_ReturnsBadRequest()
    {
        var request = new QuizPublishRequest
        {
            DocumentId = 1,
            Title = "Cards",
            ActivityType = "Flashcard"
        };
        _service.Setup(service => service.PublishAsync(10, request))
            .ThrowsAsync(new ArgumentException("Invalid flashcard."));

        var result = await _sut.Publish(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid flashcard.", badRequest.Value);
    }

    [Fact]
    public async Task GetMine_ReturnsCurrentUsersQuizzes()
    {
        var response = new[] { new QuizResponse { Id = 1, ActivityType = "Flashcard" } };
        _service.Setup(service => service.GetMineAsync(10, "Flashcard")).ReturnsAsync(response);

        var result = await _sut.GetMine("Flashcard");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task GetDetail_MissingOrNotOwned_ReturnsNotFound()
    {
        _service.Setup(service => service.GetDetailAsync(7, 10))
            .ReturnsAsync((QuizDetailResponse?)null);

        var result = await _sut.GetDetail(7);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
