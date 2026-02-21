using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskStorm.Data;
using TaskStorm.Model;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class ProjectServiceTest
{
    private PostgresqlDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PostgresqlDbContext(options);
        db.Projects.RemoveRange(db.Projects);
        db.SaveChanges();

        return db;
    }

    private ILogger<ProjectService> GetLoggerStub() =>
        new LoggerFactory().CreateLogger<ProjectService>();

    [Fact]
    public async Task CreateProject_ShouldAddProject()
    {
        var db = GetInMemoryDb();
        var logger = GetLoggerStub();
        var service = new ProjectService(db, logger);

        var request = new CreateProjectRequest("TEST", "Test project");
        var project = await service.CreateProject(request);

        Assert.NotNull(project);
        Assert.Equal("TEST", project.ShortName);
        Assert.Equal("Test project", project.Description);
        Assert.Single(await db.Projects.ToListAsync());
    }

    [Fact]
    public async Task<Project> GetProjectById_ShouldReturnProject()
    {
        var db = GetInMemoryDb();
        var logger = GetLoggerStub();
        var service = new ProjectService(db, logger);

        var project = new Model.IssueFolder.Project(2, "TEST2", "Test project2", DateTime.UtcNow);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var fetchedProject = await service.GetProjectById(project.Id);
        Assert.NotNull(fetchedProject);
        Assert.Equal(project.Id, fetchedProject.Id);
        return fetchedProject;
    }
}
