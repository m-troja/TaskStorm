using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStorm.Data;
using TaskStorm.Exception;
using TaskStorm.Exception.IssueException;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Entity.Masterdata;
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

    private readonly ILoggerFactory _loggerFactory;

    public IssueServiceTests()
    {

        using var db = GetDb();
        db.Database.EnsureCreated();

        _loggerFactory = LoggerFactory.Create(builder => { });
    }


    private PostgresqlDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x =>
                x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        return new PostgresqlDbContext(options);
    }

    private IssueService CreateService(PostgresqlDbContext db)
    {
        return new IssueService(
            db,
            _userMock.Object,
            new CommentCnv(_loggerFactory.CreateLogger<CommentCnv>()),
            new IssueCnv(
                new CommentCnv(_loggerFactory.CreateLogger<CommentCnv>()),
                _loggerFactory.CreateLogger<IssueCnv>(),
                new TeamCnv(_loggerFactory.CreateLogger<TeamCnv>())
            ),
            _loggerFactory.CreateLogger<IssueService>(),
            _activityMock.Object,
            _slackMock.Object
        );
    }

    private async Task<User> SeedUser(
        PostgresqlDbContext db,
        int id,
        string firstName = "John",
        string lastName = "Doe",
        string? slackUserId = null)
    {
        var existing = await db.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (existing != null)
            return existing;

        var user = new User(firstName, lastName)
        {
            Id = id,
            SlackUserId = slackUserId
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<Project> SeedProject(
        PostgresqlDbContext db,
        int id,
        string shortName = "PR")
    {
        var existing = await db.Projects.FirstOrDefaultAsync(x => x.Id == id);

        if (existing != null)
            return existing;

        var project = new Project(shortName, $"Project {id}")
        {
            Id = id
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return project;
    }

    private async Task<Team> SeedTeam(
        PostgresqlDbContext db,
        int id,
        string name = "Backend")
    {
        var existing = await db.Teams.FirstOrDefaultAsync(x => x.Id == id);

        if (existing != null)
            return existing;

        var team = new Team(name)
        {
            Id = id
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }

    private async Task<Issue> SeedIssue(
        PostgresqlDbContext db,
        int issueId = 1,
        int projectId = 1,
        int authorId = 1,
        int? assigneeId = 1,
        IssueStatus status = IssueStatus.NEW,
        IssuePriority priority = IssuePriority.NORMAL,
        string title = "OldTitle",
        string description = "OldDesc")
    {
        var author = await SeedUser(db, authorId);
        User? assignee = null;

        if (assigneeId.HasValue)
            assignee = await SeedUser(db, assigneeId.Value);

        var project = await SeedProject(db, projectId, $"PR{projectId}");

        var issue = new Issue
        {
            Id = issueId,
            IdInsideProject = issueId,
            Title = title,
            Description = description,
            Priority = priority,
            Status = status,
            AuthorId = author.Id,
            Author = author,
            AssigneeId = assignee?.Id,
            Assignee = assignee,
            ProjectId = project.Id,
            Project = project,
            CreatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            Labels = new List<MasterdataValue>(),
            Comments = new List<Comment>()
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var key = new Key(project, issue);

        issue.Key = key;

        db.Keys.Add(key);

        await db.SaveChangesAsync();

        return issue;
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

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateTitle()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Title = "NewTitle"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal("NewTitle", result.Title);

        _activityMock.Verify(
            x => x.UpdateTitleAsync(
                "OldTitle",
                "NewTitle",
                issue.Id,
                1),
            Times.Once);

        _slackMock.Verify(
            x => x.SendUpdateTitleAsync(
                It.IsAny<Issue>(),
                It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateStatus()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Status = "IN_PROGRESS"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(IssueStatus.IN_PROGRESS, result.Status);

        _activityMock.Verify(
            x => x.UpdateStatusAsync(
                IssueStatus.NEW,
                IssueStatus.IN_PROGRESS,
                issue.Id,
                1),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdatePriority()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Priority = "HIGH"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(IssuePriority.HIGH, result.Priority);

        _activityMock.Verify(
            x => x.UpdatePriorityAsync(
                IssuePriority.NORMAL,
                IssuePriority.HIGH,
                issue.Id,
                1),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateDescription()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            Description = "NewDesc"
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal("NewDesc", result.Description);

        _activityMock.Verify(
            x => x.UpdateDescriptionAsync(
                "OldDesc",
                "NewDesc",
                issue.Id,
                1),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateAssignee()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        await SeedUser(db, 2, "Jane", "Ops");

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            AssigneeId = 2
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(2, result.AssigneeId);

        _activityMock.Verify(
            x => x.UpdateAssigneeAsync(
                1,
                2,
                issue.Id,
                1),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateTeam()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var team = await SeedTeam(db, 5);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            TeamId = team.Id
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(team.Id, result.TeamId);

        _activityMock.Verify(
            x => x.UpdateTeamAsync(
                It.IsAny<int>(),
                team.Id,
                issue.Id,
                1),
            Times.Once);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldUpdateDueDate()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var dueDate = DateTime.UtcNow.AddDays(10);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id,
            DueDate = dueDate.ToString("yyyy-MM-dd")
        };

        var result = await service.HandleUpdateIssueRequestAsync(req, 1);

        Assert.Equal(dueDate.Date, result.DueDate!.Value.Date);
    }

    [Fact]
    public async Task HandleUpdateIssueRequestAsync_ShouldNotTriggerActivities_WhenNoChanges()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var req = new UpdateIssueRequest
        {
            IssueId = issue.Id
        };

        await service.HandleUpdateIssueRequestAsync(req, 1);

        _activityMock.VerifyNoOtherCalls();
        _slackMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateIssueAsync_ShouldCreateIssue()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await SeedUser(db, 1);
        await SeedProject(db, 1);

        var req = CreateIssueReq(
            title: "New Issue",
            authorId: 1,
            assigneeId: null,
            projectId: 1
        );

        var result = await service.CreateIssueAsync(req);

        Assert.NotNull(result);

        Assert.Equal("New Issue", result.Title);

        Assert.Equal(1, result.AuthorId);

        Assert.Single(db.Issues);

        Assert.Single(db.Keys);
    }

    [Fact]
    public async Task CreateIssueAsync_ShouldThrow_WhenTitleEmpty()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await SeedUser(db, 1);
        await SeedProject(db, 1);

        var req = CreateIssueReq(
            title: "",
            authorId: 1,
            projectId: 1
        );

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateIssueAsync(req));
    }

    [Fact]
    public async Task GetIssueByIdAsync_ShouldReturnIssue()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        var result = await service.GetIssueByIdAsync(issue.Id);

        Assert.NotNull(result);

        Assert.Equal(issue.Id, result.Id);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ShouldThrow_WhenIssueNotFound()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await Assert.ThrowsAsync<IssueNotFoundException>(() =>
            service.GetIssueByIdAsync(999));
    }

    [Fact]
    public async Task GetAllIssues_ShouldReturnAllIssues()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await SeedIssue(db, 1, 1);
        await SeedIssue(db, 2, 2);

        var issues = await service.GetAllIssues();

        Assert.Equal(2, issues.Count());
    }

    [Fact]
    public async Task DeleteIssueAsync_ShouldDeleteIssue()
    {
        using var db = GetDb();

        var service = CreateService(db);

        var issue = await SeedIssue(db);

        await service.DeleteIssueByIdAsync(issue.Id, 1);

        var deleted = await db.Issues.FindAsync(issue.Id);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task AssignIssueBySlackAsync_ShouldAssignIssue()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await SeedUser(db, 1, "John", "Dev");

        var assignee = await SeedUser(
            db,
            2,
            "Jane",
            "Ops",
            "U_ASSIGNEE");

        var issue = await SeedIssue(db);

        _userMock
            .Setup(x => x.GetIdBySlackUserId("U_ASSIGNEE"))
            .ReturnsAsync(assignee.Id);

        var req = new AssignIssueRequestChatGpt(
            issue.Key!.KeyString,
            "U_ASSIGNEE");

        var result = await service.AssignIssueBySlackAsync(req, 1);

        Assert.Equal(2, result.AssigneeId);

        _activityMock.Verify(
            x => x.UpdateAssigneeAsync(
                1,
                2,
                issue.Id,
                1),
            Times.Once);
    }

    [Fact]
    public async Task SearchIssueAsync_ShouldFilterByProjectAndStatus()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await SeedIssue(
            db,
            issueId: 1,
            projectId: 1,
            status: IssueStatus.NEW);

        await SeedIssue(
            db,
            issueId: 2,
            projectId: 1,
            status: IssueStatus.DONE);

        await SeedIssue(
            db,
            issueId: 3,
            projectId: 2,
            status: IssueStatus.IN_PROGRESS);

        var criteria = new IssueSearchCriteria
        {
            ProjectId = 1,
            Status = IssueStatus.NEW
        };

        var result = await service.SearchIssuesAsync(criteria);

        Assert.Single(result.Items);

        Assert.Equal(1, result.Items.First().Id);

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task SearchIssueAsync_ShouldSortDescending()
    {
        using var db = GetDb();

        var service = CreateService(db);

        await SeedIssue(db, issueId: 1, title: "A");
        await SeedIssue(db, issueId: 2, title: "B");

        var criteria = new IssueSearchCriteria
        {
            IsDescending = true
        };

        var result = await service.SearchIssuesAsync(criteria);

        Assert.Equal(2, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchIssueAsync_ShouldPaginate()
    {
        using var db = GetDb();

        var service = CreateService(db);

        for (int i = 1; i <= 10; i++)
        {
            await SeedIssue(db, issueId: i);
        }

        var criteria = new IssueSearchCriteria
        {
            PageNumber = 2,
            PageSize = 3
        };

        var result = await service.SearchIssuesAsync(criteria);

        Assert.Equal(3, result.Items.Count());

        Assert.Equal(10, result.TotalCount);
    }
}
