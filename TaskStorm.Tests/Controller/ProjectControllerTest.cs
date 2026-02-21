using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Model.Entity;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;

public class ProjectControllerTest
{
    private readonly Mock<IProjectService> _projectMock = new ();
    private readonly CommentCnv _commentCnv;
    private readonly ProjectCnv _projectCnv;
    private readonly IssueCnv _issueCnv;
    private readonly TeamCnv _teamCnv;
      
    public ProjectControllerTest()
    {
        var loggerFactory = LoggerFactory.Create(builder => { } );

        _commentCnv = new CommentCnv(loggerFactory.CreateLogger<CommentCnv>());
        _teamCnv = new TeamCnv(loggerFactory.CreateLogger<TeamCnv>());
        _issueCnv = new IssueCnv(_commentCnv, loggerFactory.CreateLogger<IssueCnv>(), _teamCnv);
        _projectCnv = new ProjectCnv(_issueCnv);
    }

    private ProjectController CreateController()
    {
        return new ProjectController(
            _projectMock.Object, _projectCnv, LoggerFactory.Create(b => { }).CreateLogger<ProjectController>() );
    }

    [Fact]
    public async Task GetProjectById_ShouldReturnProjectDto_WhenProjectExists()
    {
        // given
        var controller = CreateController();

        var project = new Project
        {
            Id = 10,
            ShortName = "TEST",
            Description = "Description",
            CreatedAt = DateTime.Parse("2025-01-01"),
            Issues = BuildListOfIssues()
        };

        _projectMock.Setup(s => s.GetProjectById(10))
          .ReturnsAsync(project);

        // when
        var result = await controller.GetProjectById(10);

        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectDto>(ok.Value);

        Assert.Equal(10, dto.Id);
        Assert.Equal("TEST", dto.ShortName);
        Assert.Equal("Description", dto.Description);
        Assert.Equal(2, dto.Issues.Count);
    }

    [Fact]
    public async Task GetAllProjects_ShouldReturnProjectDtoList_WhenProjectsExist()
    {
        // given
        var controller = CreateController();

        var projects = new List<Project>
        {
            new Project
            {
                Id = 1,
                ShortName = "AAA",
                Description = "D1",
                CreatedAt = DateTime.Parse("2025-01-01"),
                Issues = new List<Issue>()
            },
            new Project
            {
                Id = 2,
                ShortName = "BBB",
                Description = "D2",
                CreatedAt = DateTime.Parse("2025-01-02"),
                Issues = new List<Issue>()
            }
        };

        _projectMock.Setup(s => s.GetAllProjectsAsync())
          .ReturnsAsync(projects);

        // when
        var result = await controller.GetAllProjects();

        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtoList = Assert.IsType<List<ProjectDto>>(ok.Value);

        Assert.Equal(2, dtoList.Count);
        Assert.Equal("AAA", dtoList[0].ShortName);
        Assert.Equal("BBB", dtoList[1].ShortName);
    }
    
    [Fact]
    public async Task CreateProject_ShouldReturnCreatedProject()
    {
        // given
        var controller = CreateController();

        var request = new CreateProjectRequest("NEW", "Description");
        var project = new Project
        {
            Id = 99,
            ShortName = "NEW",
            Description = "Description",
            CreatedAt = DateTime.Parse("2025-01-01")
        };
        var createdDto = new ProjectDto
        {
            Id = 99,
            ShortName = "NEW",
            Description = "Description",
            CreatedAt = DateTime.Parse("2025-01-01")
        };

        _projectMock.Setup(s => s.CreateProject(request))
          .ReturnsAsync(project);

        // when
        var result = await controller.CreateProject(request);

        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectDto>(ok.Value);

        Assert.Equal(99, dto.Id);
        Assert.Equal("NEW", dto.ShortName);
        Assert.Equal("Description", dto.Description);
    }

    private List<Issue> BuildListOfIssues()
    {
        var project = new Project() { Id = 1, ShortName = "PROJ" };
        var key1 = new Key() { Id = 1, KeyString = "PROJ-1", Project = project, ProjectId = project.Id };
        var key2 = new Key() { Id = 2, KeyString = "PROJ-2", Project = project, ProjectId = project.Id };

        var issue1 = new Issue(
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
        {
            Id = 1,
            CreatedAt = DateTime.Parse("2025-01-02"),
            UpdatedAt = DateTime.Parse("2025-01-03"),
            Comments = new List<Comment>() { },
            Key = key1
        };

        var issue2 = new Issue(
            "Title2",
            "Desc2",
            IssuePriority.NORMAL,
            new User { Id = 3, FirstName = "John", LastName = "Doe" },
            new User { Id = 4, FirstName = "John", LastName = "Doe" },
            DateTime.Parse("2025-11-01"),
            1,
            2,
            1,
            2
            )
        {
            Id = 2,
            CreatedAt = DateTime.Parse("2025-11-02"),
            UpdatedAt = DateTime.Parse("2025-11-03"),
            Comments = new List<Comment>() { },
            Key = key2
        };
        return new List<Issue> { issue1, issue2 };
    }
}