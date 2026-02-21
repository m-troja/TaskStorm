using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private ChatGptController CreateController()
    {
       return new ChatGptController(
           _userMock.Object,
            LoggerFactory.Create(b => { }).CreateLogger<ChatGptController>(),
            _issueMock.Object,
            _issueCnv
        );
    }
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
    }
    [Fact]
    public async Task GetUserBySlackUserId_ShouldReturnUserDto_WhenUserExists()
    {
        // given
        var controller = CreateController();

        var slackUserId = "U12345678";
        var expectedUserDto = BuildUserDto(slackUserId);
        _userMock.Setup(service => service.GetUserBySlackUserIdAsync(slackUserId))
            .ReturnsAsync(expectedUserDto);
        
        // when
        var result = await controller.GetUserBySlackUserId(slackUserId);

        // then
        Assert.Equal(expectedUserDto, result);
    }

    [Fact]
    public async Task CreateIssueBySlack_ShouldCreateIssue()
    {
        // given
        var controller = CreateController();
        var req = BuildSlackCreateIssueRequest();
        var expectedIssueDto = BuildIssueDtoChatGpt(req);
        _issueMock.Setup(service => service.CreateIssueBySlackAsync(req)).ReturnsAsync(expectedIssueDto);

        // when
        var result = await controller.CreateIssueBySlack(req);

        // then
        Assert.Equal(expectedIssueDto, result);
    }

    [Fact]
    public async Task AssignIssueByChatGpt_ShouldReturnAssignedDto()
    {
        // given
        var controller = CreateController();
        var req = new AssignIssueRequestChatGpt("PROJ-1", "U12345678");
        var expectedIssue = BuildIssue(req);
        var expectedConvertedIssue = _issueCnv.ConvertIssueToIssueDtoChatGpt(expectedIssue);

        _issueMock.Setup(service => service.AssignIssueBySlackAsync(req)).ReturnsAsync(expectedIssue);

        // when
        var result = await controller.AssignIssueByChatGpt(req);

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
            1
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

}
