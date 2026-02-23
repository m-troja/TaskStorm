using Microsoft.Extensions.Logging;
using Moq;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;
public class IssueControllerTest
{
    private readonly Mock<IIssueService> mi;
    private CommentCnv commentCnv;
    public ILogger<IssueController> GetLogger() =>
        new LoggerFactory().CreateLogger<IssueController>();

    private IssueController CreateController(  Mock<IIssueService> mi)
    {
        ILogger<IssueCnv> mockIssueService = new LoggerFactory().CreateLogger<IssueCnv>();
        var teamCnvLogger = new LoggerFactory().CreateLogger<TeamCnv>();
        var teamCnv = new TeamCnv(teamCnvLogger);
        var commentLogger = new LoggerFactory().CreateLogger<CommentCnv>();
        var commentCnv = new CommentCnv(commentLogger);
        var issueCnv = new IssueCnv(commentCnv, mockIssueService, teamCnv);
        return new IssueController(mi.Object, GetLogger(), issueCnv);
    }

    [Fact]
    public async Task GetAllIssues_ShouldReturnIssues_WhenIssuesExist()
    {
        // given
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);
        var expectedIssues = new List<Model.DTO.IssueDto>
        {
            BuildIssueDto(id),
            BuildIssueDto(id++)
        };
        mi.Setup(s => s.GetAllIssues())
          .ReturnsAsync(expectedIssues);
        // when
        var result = await controller.GetAllIssues();
        // then
        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returnIssues = Assert.IsType<List<Model.DTO.IssueDto>>(ok.Value);
        Assert.Equal(expectedIssues.Count, returnIssues.Count);
        Assert.Equal(expectedIssues, returnIssues);
    }

    [Fact]
    public async Task GetIssueById_ShouldReturnIssue_WhenExists()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var issue = BuildIssueDto(id);

        mi.Setup(s => s.GetIssueDtoByIdAsync(1)).ReturnsAsync(issue);

        var result = await controller.GetIssueById(1);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returnIssue = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal(issue, returnIssue);
    }

    [Fact]
    public async Task GetIssueByKey_ShouldReturnIssue_WhenExists()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var issue = BuildIssueDto(id);

        mi.Setup(s => s.GetIssueDtoByKeyAsync("ISSUE-1")).ReturnsAsync(issue);

        var result = await controller.GetIssueByKey("ISSUE-1");

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal(issue, returned);
    }

    [Fact]
    public async Task CreateIssue_ShouldReturnCreatedResponse()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var req = new CreateIssueRequest("Hello", "desc", "NORMAL", 10, 10, null, 100);
        var project = new Project("PROJ", "Desc" );
        var issue = BuildIssue(id);

        mi.Setup(s => s.CreateIssueAsync(req))
            .ReturnsAsync(issue);

        var result = await controller.CreateIssue(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal("PROJ-1", returned.Key);
    }

    [Fact]
    public async Task AssignIssue_ShouldReturnUpdatedIssue()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var req = new AssignIssueRequest(1, 20);

        var issueDto = BuildIssueDto(id);

        mi.Setup(s => s.AssignIssueAsync(req)).ReturnsAsync(BuildIssue(id));

        mi.Setup(s => s.GetIssueDtoByIdAsync(1)).ReturnsAsync(issueDto);

        var result = await controller.AssignIssue(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);
    }

    [Fact]
    public async Task RenameIssue_ShouldReturnUpdatedIssue()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var req = new RenameIssueRequest(1, "NewTitle");

        var issueDto = BuildIssueDto(id);

        mi.Setup(s => s.RenameIssueAsync(req)).ReturnsAsync(issueDto);

        var result = await controller.RenameIssue(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal("NewTitle", returned.Title);
    }

    [Fact]
    public async Task AssignTeam_ShouldReturnUpdatedIssue()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var req = new AssignTeamRequest(1, 50);

        var issueDto = BuildIssueDto(id);

        mi.Setup(s => s.AssignTeamAsync(req)).ReturnsAsync(issueDto);

        var result = await controller.AssignTeam(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal("New Team", returned.Team.Name);
    }

    [Fact]
    public async Task ChangeIssueStatus_ShouldReturnUpdatedIssue()
    {
        int id = 1;

        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);
        var teamCnvLogger = new LoggerFactory().CreateLogger<TeamCnv>();
        var teamCnv = new TeamCnv(teamCnvLogger);
        var req = new ChangeIssueStatusRequest(1, "IN_PROGRESS");

        var issueDto = BuildIssueDto(id);


        mi.Setup(s => s.ChangeIssueStatusAsync(req)).ReturnsAsync(issueDto);

        var result = await controller.ChangeIssueStatus(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal(IssueStatus.DONE, returned.Status);
    }

    [Fact]
    public async Task UpdateIssuePriority_ShouldReturnUpdatedIssue()
    {
        int id = 1;

        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var req = new ChangeIssuePriorityRequest(1, "HIGH");

        var issueDto = BuildIssueDto(id); 

        mi.Setup(s => s.ChangeIssuePriorityAsync(req)).ReturnsAsync(issueDto);

        var result = await controller.UpdateIssuePriority(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal(IssuePriority.LOW, returned.Priority);
    }

    [Fact]
    public async Task UpdateDueDate_ShouldReturnUpdatedIssue()
    {
        int id = 1;
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var req = new UpdateDueDateRequest(1, DateTime.Parse("2030-01-01"));

        var issueDto = BuildIssueDto(id);

        mi.Setup(s => s.UpdateDueDateAsync(req)).ReturnsAsync(issueDto);

        var result = await controller.UpdateDueDate(req);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var returned = Assert.IsType<IssueDto>(ok.Value);

        Assert.Equal(DateTime.Parse("2030-01-01"), returned.DueDate);
    }

    [Fact]
    public async Task GetAllIssuesByUserId_ShouldReturnIssues()
    {
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var issues = new List<IssueDto>();

        mi.Setup(s => s.GetIssuesByUserId(1)).ReturnsAsync(issues);

        var result = await controller.GetAllIssuesByUserId(1);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        Assert.Equal(issues, ok.Value);
    }

    [Fact]
    public async Task GetAllIssuesByProjectId_ShouldReturnIssues()
    {
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        var issues = new List<IssueDto>();

        mi.Setup(s => s.GetIssuesByProjectId(1)).ReturnsAsync(issues);

        var result = await controller.GetAllIssuesByProjectId(1);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        Assert.Equal(issues, ok.Value);
    }

    [Fact]
    public async Task DeleteIssueById_ShouldReturnConfirmation()
    {
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        mi.Setup(s => s.DeleteIssueByIdAsync(1)).Returns(Task.CompletedTask);

        var result = await controller.DeletelIssueById(1);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);

        Assert.Equal("Deleted issue 1", ok.Value);
    }

    [Fact]
    public async Task DeleteAllIssues_ShouldReturnConfirmation()
    {
        var mi = new Mock<IIssueService>();
        var controller = CreateController(mi);

        mi.Setup(s => s.deleteAllIssues()).Returns(Task.CompletedTask);

        var result = await controller.DeleteAllIssues();

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);

        Assert.Equal("All issues deleted successfully", ok.Value);
    }

    private Issue BuildIssue(int id)
    {
        var project = new Project() { Id = id, ShortName = "PROJ" };
        var key = new Key() { Id = id, KeyString = "PROJ-1", Project = project, ProjectId = project.Id };

        return new Issue(
            "Title",
            "Desc",
            IssuePriority.HIGH,
            new User { Id = id, FirstName = "John", LastName = "Doe" },
            new User { Id = id++, FirstName = "John", LastName = "Doe" },
            DateTime.Parse("2025-01-01"),
            1,
            2,
            1,
            2
            )
        {
            Id = 1,
            CreatedAt = DateTime.Parse("2025-01-02"),
            UpdatedAt = DateTime.Parse("2025-01-03"),
            Comments = new List<Comment>() { },
            Key = key
        };
    }
    private User BuildUser()
    {
        return new User
        (
            "John",
            "Doe",
            "test@test.com",
            "password",
            new byte[] { Byte.Parse("111") },
            new Role(Role.ROLE_USER)
        );
    }
    
    private IssueDto BuildIssueDto(int id)
    {
        return new IssueDto(id, "ISSUE-1", "NewTitle", "Desc", IssueStatus.DONE,
                    IssuePriority.LOW, 1, 2, DateTime.Now, DateTime.Parse("2030-01-01"), DateTime.Now, new List<CommentDto>(), 1, new TeamDto(1, "New Team", new List<int>(1), new List<int>(1)));    
    }
}
