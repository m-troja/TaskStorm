using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStorm.Data;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class TeamServiceTests
{
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IProjectService> _projectMock = new();
    private readonly Mock<ITeamService> _teamMock = new();
    private readonly Mock<IActivityService> _activityMock = new();
    private readonly Mock<ISlackNotificationService> _slackMock = new();

    private readonly CommentCnv _commentCnv;
    private readonly TeamCnv _teamCnv;
    private readonly IssueCnv _issueCnv;

    public TeamServiceTests()
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

        db.Teams.RemoveRange(db.Teams);
        db.SaveChanges();

        return new PostgresqlDbContext(options);
    }

    private TeamService CreateService(PostgresqlDbContext db)
    {
        return new TeamService(db, LoggerFactory.Create(b => { }).CreateLogger<TeamService>(), _issueCnv, new UserCnv());
    }

    [Fact]
    public async Task GetAllTeamsAsync_ShouldReturnAllTeams()
    {
        var db = GetDb();
        db.Teams.Add(new Team("Team1"));
        db.Teams.Add(new Team("Team2"));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var teams = await service.GetAllTeamsAsync();

        Assert.Equal(2, teams.Count);
        Assert.Contains(teams, t => t.Name == "Team1");
        Assert.Contains(teams, t => t.Name == "Team2");
    }

    [Fact]
    public async Task GetTeamByIdAsync_ShouldReturnTeam_WhenExists()
    {
        var db = GetDb();
        var team = new Team("Team1");
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetTeamByIdAsync(team.Id);

        Assert.Equal(team.Name, result.Name);
    }

    [Fact]
    public async Task GetTeamByIdAsync_ShouldThrow_WhenNotFound()
    {
        var db = GetDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetTeamByIdAsync(999));
    }

    [Fact]
    public async Task AddTeamAsync_ShouldAddTeam_WhenNameValid()
    {
        var db = GetDb();
        var service = CreateService(db);

        var request = new CreateTeamRequest("Team1");

        var result = await service.AddTeamAsync(request);

        var teamInDb = await db.Teams.FirstOrDefaultAsync(t => t.Name == "Team1");
        Assert.NotNull(teamInDb);
        Assert.Equal("Team1", result.Name);
        Assert.Equal(teamInDb.Id, result.Id);
    }

    [Fact]
    public async Task AddTeamAsync_ShouldThrowArgumentException_WhenNameEmpty()
    {
        var db = GetDb();
        var service = CreateService(db);

        var request = new CreateTeamRequest("");

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddTeamAsync(request));
    }

    [Fact]
    public async Task AddTeamAsync_ShouldThrow_WhenNameExists()
    {
        var db = GetDb();
        db.Teams.Add(new Team("Existing"));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddTeamAsync(new CreateTeamRequest("Existing")));
    }

    [Fact]
    public async Task GetIssuesByTeamId_ShouldReturnIssues()
    {
        // Arrange
        var db = GetDb();
        var team = new Team("Team1");
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        
        var project = new Project { Description = "Project1",ShortName = "TEST" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        
        var issue = new Issue
        {
            Title = "Issue1",
            Key = new Key { KeyString = "TEST-0" },
            Status = IssueStatus.NEW,
            Priority = IssuePriority.NORMAL,
            CreatedAt = DateTime.UtcNow,
            Project = project,
            ProjectId = project.Id,
            Team = team,
            TeamId = team.Id
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var key = new Key(project, issue);
        issue.Key = key;

        db.Keys.Add(key);
        await db.SaveChangesAsync();


        var service = CreateService(db);

        // Act
        var result = await service.GetIssuesByTeamId(team.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Issue1", result[0].Title);
        Assert.Equal("TEST-0", result[0].Key);
    }


    [Fact]
    public async Task GetUsersByTeamId_ShouldReturnUsers()
    {
        // Arrange
        var db = GetDb();
        var team = new Team("Team1");
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var user = new User("John", "Doe", "john@example.com", "pw", new byte[16], new Role("USER"));
        var Teams = new List<Team> { team };
        user.Teams = Teams;

        db.Users.Add(user);
        
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetUsersByTeamId(team.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
        Assert.Equal("Doe", result[0].LastName);
        Assert.Equal("john@example.com", result[0].Email);
        Assert.Contains("USER", result[0].Roles);
    }
}
