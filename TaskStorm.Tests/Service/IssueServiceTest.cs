using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog.Core;
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
namespace TaskStorm.Tests.Service;

public class IssueServiceTests
{
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IProjectService> _projectMock = new();
    private readonly Mock<ITeamService> _teamMock = new();
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
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new PostgresqlDbContext(options);

        db.Issues.RemoveRange(db.Issues);
        db.SaveChanges();

        return new PostgresqlDbContext(options);
    }

    private ILogger<IssueService> Log()
    {
        return new LoggerFactory().CreateLogger<IssueService>();
    }

    private IssueService CreateService(PostgresqlDbContext db)
    {
        return new IssueService(
            db,
            _userMock.Object,
            _commentCnv,
            _issueCnv,
            _projectMock.Object,
            LoggerFactory.Create(b => { }).CreateLogger<IssueService>(),
            _teamMock.Object,
            _activityMock.Object,
            _slackMock.Object
        );
    }

    private async Task<Key> AddIssueWithKey(
        PostgresqlDbContext db,
        Issue issue,
        Project project)
    {
        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var key = new Key(project, issue);
        db.Keys.Add(key);
        await db.SaveChangesAsync();

        issue.Key = key;
        db.Issues.Update(issue);
        await db.SaveChangesAsync();

        return key;
    }

    [Fact]
    public async Task CreateIssueAsync_ShouldCreateIssue()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "UA") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        _userMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(user);
        _projectMock.Setup(x => x.GetProjectById(100)).ReturnsAsync(project);

        var req = new CreateIssueRequest("Hello", "desc", "NORMAL", 10, 10, null, 100);

        var created = await service.CreateIssueAsync(req);

        Assert.Equal("Hello", created.Title);
        Assert.Equal(100, created.ProjectId);
        Assert.NotNull(created.Key);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ShouldReturnIssue()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "UA") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "X", Author = user, ProjectId = 100, Project = project };
        var key = await AddIssueWithKey(db, issue, project);

        _projectMock.Setup(x => x.GetProjectById(100)).ReturnsAsync(project);

        var result = await service.GetIssueByIdAsync(issue.Id);

        Assert.Equal("X", result.Title);
        Assert.Equal(issue.Id, result.Id);
    }

    [Fact]
    public async Task AssignIssueAsync_ShouldChangeAssignee()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user1 = new User("Author", "U1") { Id = 10 };
        var user2 = new User("NewAssignee", "U2") { Id = 20 };
        db.Users.Add(user1);
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        _userMock.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(user2);
        _userMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(user1);

        var project = new Project("PROJ", "Test Project") { Id = 200 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        
        _projectMock.Setup(x => x.GetProjectById(200)).ReturnsAsync(project);

        var issue = new Issue
        {
            Id = 1,
            Title = "Old",
            AssigneeId = 10,
            ProjectId = 200,
            Project = project
        };

        issue.Author = user1;
        issue.Assignee = user1; 

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var key = new Key(project, issue);
        db.Keys.Add(key);
        await db.SaveChangesAsync();

        issue.Key = key;
        db.Issues.Update(issue);
        await db.SaveChangesAsync();

        var req = new AssignIssueRequest(1, 20);
        var updated = await service.AssignIssueAsync(req);

        Assert.Equal(20, updated.AssigneeId);
    }

    [Fact]
    public async Task GetIssueDtoByIdAsync_ShouldReturnDto()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "UA") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "X", Author = user, ProjectId = 100, Project = project };
        var key = await AddIssueWithKey(db, issue, project);

        _projectMock.Setup(x => x.GetProjectById(100)).ReturnsAsync(project);

        var dto = await service.GetIssueDtoByIdAsync(issue.Id);

        Assert.Equal("X", dto.Title);
        Assert.Equal(key.KeyString, dto.Key);
    }

    [Fact]
    public async Task AssignIssueBySlackAsync_ShouldAssignCorrectUser()
    {
        var db = GetDb();
        var service = CreateService(db);

        var author = new User("A", "U1") { Id = 10 };
        var user2 = new User("B", "U2") { Id = 20 };

        db.Users.Add(author);
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        _userMock.Setup(x => x.GetIdBySlackUserId("U2")).ReturnsAsync(20);
        _userMock.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(user2);
        _userMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(author);
        _activityMock.Setup(x => x.CreateActivityPropertyUpdatedAsync(ActivityType.UPDATED_ASSIGNEE, "10", "20", 1)).ReturnsAsync(new ActivityPropertyUpdated("1", "2", 1, ActivityType.UPDATED_ASSIGNEE));

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Id = 1, IdInsideProject = 1, Title = "Test Title", Author = author, Assignee = author, AssigneeId = 10, Project = project, ProjectId = 100 };
        var key = await AddIssueWithKey(db, issue, project);

        var req = new AssignIssueRequestChatGpt(key.KeyString, "U2");

        var updated = await service.AssignIssueBySlackAsync(req);

        Assert.Equal(20, updated.AssigneeId);
    }

    [Fact]
    public async Task RenameIssueAsync_ShouldUpdateTitle()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "Old", Author = user, Project = project, ProjectId = 100 };
        var key = await AddIssueWithKey(db, issue, project);

        var req = new RenameIssueRequest(issue.Id, "NewTitle");

        var dto = await service.RenameIssueAsync(req);

        Assert.Equal("NewTitle", dto.Title);
    }

    [Fact]
    public async Task ChangeIssueStatusAsync_ShouldUpdateStatus()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "Test", Status = IssueStatus.NEW, Author = user, Project = project, ProjectId = 100 };
        await AddIssueWithKey(db, issue, project);

        var req = new ChangeIssueStatusRequest(issue.Id, "IN_PROGRESS");

        var dto = await service.ChangeIssueStatusAsync(req);

        Assert.Equal("IN_PROGRESS", dto.Status.ToString());
    }

    [Fact]
    public async Task ChangeIssuePriorityAsync_ShouldUpdatePriority()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "Test",  Priority = IssuePriority.NORMAL, Author = user, Project = project, ProjectId = 100 };
        await AddIssueWithKey(db, issue, project);

        var req = new ChangeIssuePriorityRequest(issue.Id, "HIGH");

        var dto = await service.ChangeIssuePriorityAsync(req);

        Assert.Equal("HIGH", dto.Priority.ToString());
    }

    [Fact]
    public async Task AssignTeamAsync_ShouldSetTeam()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var team = new Team("Backend") { Id = 50 };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        _teamMock.Setup(x => x.GetTeamByIdAsync(50)).ReturnsAsync(team);

        var issue = new Issue { Title = "Test", Author = user, Project = project, ProjectId = 100 };
        await AddIssueWithKey(db, issue, project);

        var req = new AssignTeamRequest(issue.Id, 50);

        var dto = await service.AssignTeamAsync(req);

        Assert.Equal("Backend", dto.Team?.Name);
    }

    [Fact]
    public async Task UpdateDueDateAsync_ShouldSetDueDate()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "Test",  Author = user, Project = project, ProjectId = 100 };
        await AddIssueWithKey(db, issue, project);

        var date = DateTime.UtcNow.AddDays(2);
        var dto = await service.UpdateDueDateAsync(new UpdateDueDateRequest(issue.Id, date));

        Assert.Equal(date, dto.DueDate);
    }

    [Fact]
    public async Task GetAllIssues_ShouldReturnAll()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await AddIssueWithKey(db,
            new Issue { Title = "1", Author = user, Project = project, ProjectId = 100 },
            project
        );

        await AddIssueWithKey(db,
            new Issue { Title = "2", Author = user, Project = project, ProjectId = 100 },
            project
        );

        var issues = await service.GetAllIssues();

        Assert.Equal(2, issues.Count());
    }

    [Fact]
    public async Task GetIssueIdFromKey_ShouldReturnCorrectId()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var issue = new Issue { Title = "Test",  IdInsideProject = 7, Author = user, Project = project, ProjectId = 100 };
        var key = await AddIssueWithKey(db, issue, project);

        var id = await service.GetIssueIdFromKey(key.KeyString);

        Assert.Equal(issue.Id, id);
    }

    [Fact]
    public async Task GetAllIssuesByProjectId_ShouldReturnIssuesForProject()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        _projectMock.Setup(x => x.GetProjectById(100)).ReturnsAsync(project);

        await AddIssueWithKey(db, new Issue { Title = "1", Author = user, Project = project, ProjectId = 100 }, project);

        var result = await service.GetIssuesByProjectId(100);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllIssuesByUserId_ShouldReturnIssuesForUser()
    {
        var db = GetDb();
        var service = CreateService(db);

        var user = new User("A", "U1") { Id = 10 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        _userMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(user);

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await AddIssueWithKey(db, new Issue { Title = "Test",  Assignee = user, AssigneeId = 10, Author = user, Project = project, ProjectId = 100 }, project);

        var result = await service.GetIssuesByUserId(10);

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateIssueBySlackAsync_ShouldCreateIssue()
    {
        var db = GetDb();
        var service = CreateService(db);

        _userMock.Setup(x => x.GetIdBySlackUserId("AU")).ReturnsAsync(10);
        _userMock.Setup(x => x.GetIdBySlackUserId("BU")).ReturnsAsync(20);

        var userA = new User("A", "AU") { Id = 10 };
        var userB = new User("B", "BU") { Id = 20 };
        db.Users.AddRange(userA, userB);
        await db.SaveChangesAsync();

        var project = new Project("PR", "Test") { Id = 100 };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        _projectMock.Setup(x => x.GetProjectById(100)).ReturnsAsync(project);

        var req = new SlackCreateIssueRequest(
            "Title",
            "Desc",
            "NORMAL",
            "AU",
            "BU",
            null,
            100
        );

        var result = await service.CreateIssueBySlackAsync(req);

        Assert.Equal("Title", result.Title);
        Assert.Equal("PR-", result.Key.Substring(0, 3));
    }

}