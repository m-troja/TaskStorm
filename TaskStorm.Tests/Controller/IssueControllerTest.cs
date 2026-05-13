using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using Xunit;

public class IssueControllerTests
{
    private readonly Mock<IIssueService> _issueService = new();
    private readonly Mock<IActivityService> _activityService = new();

    private ActivityCnv _activityCnv;
    private IssueCnv _issueCnv;

    private IssueController CreateController()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var commentCnvLogger = loggerFactory.CreateLogger<CommentCnv>();
        var teamCnvLogger = loggerFactory.CreateLogger<TeamCnv>();
        var issueCnvLogger = loggerFactory.CreateLogger<IssueCnv>();
        var activityCnvLogger = loggerFactory.CreateLogger<ActivityCnv>();

        _activityCnv = new ActivityCnv(activityCnvLogger);
        _issueCnv = new IssueCnv(new CommentCnv(commentCnvLogger), issueCnvLogger, new TeamCnv(teamCnvLogger));

        var controllerLogger = loggerFactory.CreateLogger<IssueController>();

        var controller = new IssueController(
            _issueService.Object,
            controllerLogger,
            _issueCnv,
            _activityService.Object,
            _activityCnv
        );

        // Setup HttpContext with a user
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1")
                }))
            }
        };

        return controller;
    }



    private Activity BuildActivity(ActivityType type)
    {
        return type switch
        {
            ActivityType.CREATED_ISSUE => new ActivityPropertyCreated(type, 1, 1),
            ActivityType.UPDATED_STATUS => new ActivityPropertyUpdated("DONE", "IN_PROGRESS", 1, type, 1),
            _ => new ActivityPropertyCreated(type, 1, 1)
        };
    }

    [Fact]
    public async Task CreateIssue_ShouldReturnOk()
    {
        var controller = CreateController();
        var req = new CreateIssueRequest("Test", "Desc", "NORMAL", 1, null, null, 1);
        var issue = BuildIssue().Result;

        var dto = _issueCnv.EntityToDto(issue);

        _issueService.Setup(x => x.CreateIssueAsync(req)).ReturnsAsync(issue);

        var result = await controller.CreateIssue(req);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto.Id, ((IssueDto)ok.Value).Id);
    }

    [Fact]
    public async Task GetIssueById_ShouldReturnIssue()
    {
        var controller = CreateController();
        var dto = BuildIssueDto(1);

        _issueService.Setup(x => x.GetIssueDtoByIdAsync(1)).ReturnsAsync(dto);

        var result = await controller.GetIssueById(1);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto.Id, ((IssueDto)ok.Value).Id);
    }

    [Fact]
    public async Task GetIssueByKey_ShouldReturnIssue()
    {
        var controller = CreateController();
        var dto = BuildIssueDto(1);

        _issueService.Setup(x => x.GetIssueDtoByKeyAsync("ISSUE-1")).ReturnsAsync(dto);

        var result = await controller.GetIssueByKey("ISSUE-1");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto.Id, ((IssueDto)ok.Value).Id);
    }

    [Fact]
    public async Task GetIssueByKey_ShouldThrow_WhenKeyEmpty()
    {
        var controller = CreateController();
        await Assert.ThrowsAsync<TaskStorm.Exception.BadRequestException>(() => controller.GetIssueByKey(""));
    }

    [Fact]
    public async Task GetAllIssues_ShouldReturnList()
    {
        var controller = CreateController();
        var list = new List<IssueDto> { BuildIssueDto(1), BuildIssueDto(2) };

        _issueService.Setup(x => x.GetAllIssues()).ReturnsAsync(list);

        var result = await controller.GetAllIssues();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var issues = Assert.IsType<List<IssueDto>>(ok.Value);
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task UpdateIssue_ShouldReturnUpdatedIssue()
    {
        var controller = CreateController();
        var req = new UpdateIssueRequest { IssueId = 1, Title = "Updated" };
        var issue = BuildIssue().Result;
        var dto = BuildIssueDto(1);

        _issueService.Setup(x => x.HandleUpdateIssueRequestAsync(req, 1)).ReturnsAsync(issue);

        var result = await controller.UpdateIssue(req);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto.Id, ((IssueDto)ok.Value).Id);
    }

    [Fact]
    public async Task DeleteIssue_ShouldReturnOk()
    {
        var controller = CreateController();
        _issueService.Setup(x => x.DeleteIssueByIdAsync(1, 1)).Returns(Task.CompletedTask);

        var result = await controller.DeletelIssueById(1);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Contains("Deleted issue", ok.Value.ToString());
    }

    [Fact]
    public async Task GetActivitiesByIssueId_ShouldReturnConvertedDtos()
    {
        var controller = CreateController();
        var activities = new List<Activity> { BuildActivity(ActivityType.CREATED_ISSUE) };

        _activityService.Setup(x => x.GetActivitiesByIssueIdAsync(1)).ReturnsAsync(activities);

        var result = await controller.GetActivitiesByIssueId(1);
        var ok = Assert.IsType<OkObjectResult>(result.Result);

        var dtos = Assert.IsType<List<ActivityDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal(ActivityType.CREATED_ISSUE, dtos[0].ActivityType);
        Assert.Equal(1, dtos[0].IssueId);
    }

    private async Task<Issue> BuildIssue()
    {
        var user = new User("A", "B") { Id = 1 };
        var project = new Project("PR", "Test") { Id = 1 };
        var team = new Team("Name") { Id = 1 };
        var key = new Key { Id = 1, KeyString = "ISSUE-1" };
        var issue = new Issue
        {
            Id = 1,
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
            Team= team,
            TeamId = team.Id,
            Key = key
        };
        return issue;
    }

    private IssueDto BuildIssueDto(int id)
    {
        return new IssueDto(
            id,
            $"ISSUE-{id}",
            "NewTitle",
            "Desc",
            IssueStatus.DONE,
            IssuePriority.LOW,
            1,
            2,
            DateTime.Now,
            DateTime.Parse("2030-01-01"),
            DateTime.Now,
            1,
            1,
            new List<string> { "label"}
        );
    }

}