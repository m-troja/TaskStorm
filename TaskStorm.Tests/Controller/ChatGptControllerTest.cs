using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;
public class ChatGptControllerTest
{
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IIssueService> _issueMock = new();

    private readonly CommentCnv _commentCnv;
    private readonly IssueCnv _issueCnv;
    private readonly TeamCnv _teamCnv;
    private readonly ChatGptController _controller;

    public ChatGptControllerTest()
    {
        var loggerFactory = LoggerFactory.Create(b => { });

        _commentCnv = new CommentCnv(loggerFactory.CreateLogger<CommentCnv>());
        _teamCnv = new TeamCnv(loggerFactory.CreateLogger<TeamCnv>());
        _issueCnv = new IssueCnv(
            _commentCnv,
            loggerFactory.CreateLogger<IssueCnv>(),
            _teamCnv
        );
        _controller = new ChatGptController(
            _userMock.Object,
            loggerFactory.CreateLogger<ChatGptController>(),
            _issueMock.Object,
            _issueCnv
        );
    }

    [Fact]
    public async Task GetUserBySlackUserId_ShouldReturnUserDto_WhenUserExists()
    {
        var slackUserId = "U12345678";
        var expectedUserDto = BuildUserDto(slackUserId);
        _userMock.Setup(service => service.GetUserBySlackUserIdAsync(slackUserId))
            .ReturnsAsync(expectedUserDto);
        
        // when
        var result = await _controller.GetUserBySlackUserId(slackUserId);

        // then
        Assert.Equal(expectedUserDto, result);
    }

    [Fact]
    public async Task CreateIssueBySlack_ShouldCreateIssue()
    {
        // given
        var req = BuildSlackCreateIssueRequest();
        var expectedIssueDto = BuildIssueDtoChatGpt(req);
        _issueMock.Setup(service => service.CreateIssueBySlackAsync(req)).ReturnsAsync(expectedIssueDto);

        // when
        var result = await _controller.CreateIssueBySlack(req);

        // then
        Assert.Equal(expectedIssueDto, result);
    }

    [Fact]
    public async Task AssignIssueByChatGpt_ShouldReturnAssignedDto()
    {
        SetUser(1, Role.ROLE_ADMIN);

        var req = new AssignIssueRequestChatGpt("PROJ-1", "U12345678");
        var expectedIssue = BuildIssue(req);
        var expectedConvertedIssue = _issueCnv.EntityToIssueDtoChatGpt(expectedIssue);

        _issueMock.Setup(service => service.AssignIssueBySlackAsync(req, 1)).ReturnsAsync(expectedIssue);

        // when
        var result = await _controller.AssignIssueByChatGpt(req);

        // then
        Assert.Equal(expectedConvertedIssue.Id, result.Id);
        Assert.Equal(expectedConvertedIssue.Key, result.Key);
        Assert.Equal(expectedConvertedIssue.ProjectId, result.ProjectId);
        Assert.Equal(expectedConvertedIssue.AssigneeSlackId, result.AssigneeSlackId);
        Assert.Equal(expectedConvertedIssue.AuthorSlackId, result.AuthorSlackId);
    }

    private UserDto BuildUserDto(string slackUserId)
    {
        return new UserDto(
            1,
            "John",
            "Doe",
            "email@test.com",
            new List<string>() { Role.ROLE_USER },
            new List<string>() { },
            false,
            slackUserId
            );
    }
    private SlackCreateIssueRequest BuildSlackCreateIssueRequest()
    {
        return new SlackCreateIssueRequest(
            "title",
            "description",
            "HIGH",
            "U87654321",
            "U87654322",
            "2025-12-31",
            1 );
    }

    private IssueDtoChatGpt BuildIssueDtoChatGpt(SlackCreateIssueRequest req)
    {
        var parsedPriority = Enum.TryParse<IssuePriority>(req.priority, out var priority)
        ? priority
        : IssuePriority.NORMAL;

        return new IssueDtoChatGpt(
            1,
            "PROJ-1",
            req.title,
            req.description,
            Model.IssueFolder.IssueStatus.NEW,
            parsedPriority,
            req.authorSlackId,
            req.assigneeSlackId,
            DateTime.Parse("2025-01-01"),
            req.dueDate != null ? DateTime.Parse(req.dueDate) : null,
            DateTime.Parse("2025-01-01"),
            new List<CommentDto>(),
            1,
            "TeamName"
            );
    }

    private Issue BuildIssue(AssignIssueRequestChatGpt req)
    {
        var project = new Project() { Id = 1, ShortName = "PROJ" };
        var key = new Key() { Id = 1, KeyString = "PROJ-1", Project = project, ProjectId = project.Id };

        return new Issue(
            "Title",
            "Desc",
            IssuePriority.HIGH,
            new User { Id = 1, FirstName = "John", LastName = "Doe" },
            new User { Id = 2, FirstName = "John", LastName = "Doe" },
            DateTime.Parse("2025-01-01"),
            1,
            2,
            1,
            2
            )
        { Id = 1 ,
        CreatedAt = DateTime.Parse("2025-01-02"),
        UpdatedAt = DateTime.Parse("2025-01-03"),
        Comments = new List<Comment>() { } ,
        Key = key
        };
    }

    private void SetUser(int id, string role)
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ClaimTypes.Role, role)
    };

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

}
