using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStorm.Data;
using TaskStorm.Exception.IssueException;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

public class IssueServiceTests
{
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IActivityService> _activityMock = new();
    private readonly Mock<ISlackNotificationService> _slackMock = new();

    private readonly CommentCnv _commentCnv;
    private readonly TeamCnv _teamCnv;
    private readonly IssueCnv _issueCnv;

    public IssueServiceTests()
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

    private PostgresqlDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PostgresqlDbContext(options);

        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        return new PostgresqlDbContext(options);
    }

    private IssueService CreateService(PostgresqlDbContext db)
    {
        var logger = LoggerFactory.Create(b => { }).CreateLogger<IssueService>();

        return new IssueService(
            db,
            _userMock.Object,
            new CommentCnv(LoggerFactory.Create(b => { }).CreateLogger<CommentCnv>()),
            new IssueCnv(
                new CommentCnv(LoggerFactory.Create(b => { }).CreateLogger<CommentCnv>()),
                LoggerFactory.Create(b => { }).CreateLogger<IssueCnv>(),
                new TeamCnv(LoggerFactory.Create(b => { }).CreateLogger<TeamCnv>())
            ),
            logger,
            _activityMock.Object,
            _slackMock.Object
        );
    }

    private async Task<Issue> SeedIssue(PostgresqlDbContext db, int userId, int issueId, int projectId)
    {
        var user = new User("A", "B") { Id = userId };
        var project = new Project($"PR{projectId}", $"Test{projectId}") { Id = projectId };
        var key = new Key { Id = issueId, KeyString = $"ISSUE-{issueId}"};
        db.Users.Add(user);
        db.Projects.Add(project);
        db.Keys.Add(key);
        await db.SaveChangesAsync();

        var issue = new Issue
        {
            Id = issueId,
            Title = "OldTitle",
            Description = "OldDesc",
            Priority = IssuePriority.NORMAL,
            Status = IssueStatus.NEW,
            Author = user,
            AuthorId = user.Id,
            Assignee = user,
            AssigneeId = user.Id,
            Project = project,
            ProjectId = project.Id,
            Key = key
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        return issue;
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateTitle()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        _userMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(issue.Author);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Title = "NewTitle"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal("NewTitle", result.Title);

        _activityMock.Verify(
            x => x.UpdateTitleAsync("OldTitle", "NewTitle", issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendUpdateTitleAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateStatus()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Status = "IN_PROGRESS"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(IssueStatus.IN_PROGRESS, result.Status);

        _activityMock.Verify(
            x => x.UpdateStatusAsync(IssueStatus.NEW, IssueStatus.IN_PROGRESS, issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendIssueStatusChangedNotificationAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdatePriority()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Priority = "HIGH"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(IssuePriority.HIGH, result.Priority);

        _activityMock.Verify(
            x => x.UpdatePriorityAsync(IssuePriority.NORMAL, IssuePriority.HIGH, issue.Id, 1),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateDescription()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Description = "NewDesc"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal("NewDesc", result.Description);

        _activityMock.Verify(
            x => x.UpdateDescriptionAsync("OldDesc", "NewDesc", issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendUpdateDescriptionAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldNotSendSlack_WhenNoChanges()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id
        };

        await service.HandleUpdateIssueRequestAsync(req, 1);

        _slackMock.VerifyNoOtherCalls();
        _activityMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateAssignee()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var newUser = new User("New", "User") { Id = 2 };
        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        _userMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(issue.Author);
        _userMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(newUser);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            AssigneeId = 2
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(2, result.AssigneeId);

        _activityMock.Verify(
            x => x.UpdateAssigneeAsync(1, 2, issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendIssueAssignedNotificationAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateTeam()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var team = new Team("Backend") { Id = 5 };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            TeamId = team.Id
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(team.Id, result.TeamId);

        _activityMock.Verify(
            x => x.UpdateTeamAsync(It.IsAny<int>(), team.Id, issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendTeamAssignedNotificationAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateDueDate()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var newDate = DateTime.UtcNow.AddDays(5);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            DueDate = newDate.ToString("yyyy-MM-dd")
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(newDate.Date, result.DueDate.Value.Date);

        _activityMock.Verify(
            x => x.UpdateDueDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendIssueDueDateUpdatedNotificationAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldProcessMultipleChanges()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Title = "NewTitle",
            Status = "IN_PROGRESS",
            Priority = "HIGH"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal("NewTitle", result.Title);
        Assert.Equal(IssueStatus.IN_PROGRESS, result.Status);
        Assert.Equal(IssuePriority.HIGH, result.Priority);

        _activityMock.Verify(x =>
            x.UpdateTitleAsync("OldTitle", "NewTitle", issue.Id, 1),
            Times.Once);

        _activityMock.Verify(x =>
            x.UpdateStatusAsync(IssueStatus.NEW, IssueStatus.IN_PROGRESS, issue.Id, 1),
            Times.Once);

        _activityMock.Verify(x =>
            x.UpdatePriorityAsync(IssuePriority.NORMAL, IssuePriority.HIGH, issue.Id, 1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendUpdateTitleAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);

        _slackMock.Verify(
            x => x.SendIssueStatusChangedNotificationAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);

        _slackMock.Verify(
            x => x.SendIssuePriorityChangedNotificationAsync(It.IsAny<Issue>(), It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldThrow_WhenIssueNotFound()
    {
        var db = GetDb();
        var service = CreateService(db);

        var req = new UpdateIssueRequest
        {
            IssueId = 999,
            Title = "Test"
        };

        await Assert.ThrowsAsync<IssueNotFoundException>(() =>
            service.HandleUpdateIssueRequestAsync(req, 1));
    }

    [Fact]
    public async Task CreateIssueAsync_ShouldCreateIssue()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("John", "Dev") { Id = 1 };
        var project = new Project("PR", "Test") { Id = 1 };

        db.Users.Add(user);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var req = CreateIssueReq(
            title: "New Issue",
            authorId: user.Id,
            assigneeId: null,
            projectId: project.Id
        );

        var result = await service.CreateIssueAsync(req);

        Assert.NotNull(result);
        Assert.Equal("New Issue", result.Title);
        Assert.Equal(user.Id, result.AuthorId);

        Assert.Single(db.Issues);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ShouldReturnIssue()
    {
        var db = GetDb();
        var service = CreateService(db);

        var issue = await SeedIssue(db, 1, 1, 1);

        var result = await service.GetIssueByIdAsync(issue.Id);

        Assert.NotNull(result);
        Assert.Equal(issue.Id, result.Id);
        Assert.Equal(issue.Title, result.Title);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ShouldThrow_WhenIssueNotFound()
    {
        var db = GetDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<IssueNotFoundException>(() =>
            service.GetIssueByIdAsync(999));
    }

    [Fact]
    public async Task GetIssuesAsync_ShouldReturnAllIssues()
    {
        var db = GetDb();
        var service = CreateService(db);

        await SeedIssue(db, 1, 1, 1);
        await SeedIssue(db, 2, 2, 2);

        var issues = await service.GetAllIssues();

        Assert.NotNull(issues);
        Assert.True(issues.Count() >= 2);
    }

    [Fact]
    public async Task DeleteIssueAsync_ShouldDeleteIssue()
    {
        var db = GetDb();
        var service = CreateService(db);
        var user = new User("John", "Dev") { Id = 1 };

        var issue = await SeedIssue(db, 1, 1, 1);

        await service.DeleteIssueByIdAsync(issue.Id, user.Id);

        var deleted = await db.Issues.FindAsync(issue.Id);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task CreateIssueAsync_ShouldThrow_WhenTitleEmpty()
    {
        var db = GetDb();
        var service = CreateService(db);

        var req = CreateIssueReq(
            title: "",
            authorId: 1
        );

        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            service.CreateIssueAsync(req));
    }

    [Fact]
    public async Task AssignIssuesBySlackAsync_ShouldAssignIssue()
    {
        var db = GetDb();
        var service = CreateService(db);

        var author = new User("John", "Dev")
        {
            Id = 1,
            SlackUserId = "U_AUTHOR"
        };

        var assignee = new User("Jane", "Ops")
        {
            Id = 2,
            SlackUserId = "U_ASSIGNEE"
        };

        var project = new Project("PR", "Test")
        {
            Id = 1
        };

        db.Users.Add(author);
        db.Users.Add(assignee);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = await SeedIssue(db, author.Id, 1, project.Id);

        issue.Key.KeyString = "PR-1";
        issue.AssigneeId = author.Id;
        issue.Assignee = author;

        await db.SaveChangesAsync();

        _userMock
            .Setup(x => x.GetIdBySlackUserId("U_ASSIGNEE"))
            .ReturnsAsync(assignee.Id);

        var req = new AssignIssueRequestChatGpt
        ("PR-1","U_ASSIGNEE");

        var result = await service.AssignIssuesBySlackAsync(req, author.Id);

        Assert.NotNull(result);
        Assert.Equal(issue.Id, result.Id);
        Assert.Equal(assignee.Id, result.AssigneeId);

        _activityMock.Verify(
            x => x.UpdateAssigneeAsync(author.Id, assignee.Id, issue.Id, author.Id),
            Times.Once);

        _slackMock.Verify(
            x => x.SendIssueAssignedNotificationAsync(
                It.IsAny<Issue>(),
                It.IsAny<User>()),
            Times.Once);
    }


    private CreateIssueRequest CreateIssueReq(
        string title = "Test Issue",
        string description = "Desc",
        string priority = "NORMAL",
        int authorId = 1,
        int? assigneeId = 1,
        string? dueDate = null,
        int projectId = 1)
    {
        return new CreateIssueRequest(
            title,
            description,
            priority,
            authorId,
            assigneeId,
            dueDate,
            projectId
        );
    }


}