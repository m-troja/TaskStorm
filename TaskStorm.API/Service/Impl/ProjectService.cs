using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using TaskStorm.Data;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Log;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
namespace TaskStorm.Service.Impl;

public class ProjectService : IProjectService
{
    private readonly PostgresqlDbContext _db ;
    private readonly ILogger<ProjectService> l;
    private readonly int SystemProjectId = -1;
    public async Task<Project> GetProjectById(int id)
    {
        await createSystemProject();

        Project? project = await _db.Projects
            .Where(p => p.Id == id)
            .Include(p => p.Issues)
            .Include(p => p.Keys)
            .FirstOrDefaultAsync();
        if (project == null) throw new ProjectNotFoundException($"Project id {id} was not found");
        return project;
    }

    private async Task createSystemProject()
    {
        l.LogInformation("Creating system project!");
        Project? project;
        if (await _db.Projects.FirstOrDefaultAsync(p => p.Id == SystemProjectId) == null)
        {
            project = new Project { Id = -1, Description = "Dummy project", ShortName = "DUMMY" };
            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();
            l.LogInformation("Created system project");
        }
    }

    public async Task<Project> GetProjectByName(string shortName)
    {
        Project? project = await _db.Projects.Where(p => p.ShortName == shortName).FirstOrDefaultAsync();
        if (project == null) throw new ProjectNotFoundException($"Project name {shortName} was not found");
        return project;
    }

    public async Task DeleteProjectById(int id)
    {
        var project = await GetProjectById(id);
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        l.LogDebug($"Project with id {id} deleted");
    }

    public async Task<Project> CreateProject(CreateProjectRequest cpr)
    {
        if (cpr.description == null) cpr = cpr with { description = "" };
        if (cpr.description.Length > 255) throw new InvalidProjectData("Project short name cannot be longer than 255 characters");
       
        string shortNameTrimmed = cpr.shortName.Trim().ToUpper();
        if (shortNameTrimmed.Length == 0 || string.IsNullOrWhiteSpace(cpr.description)) throw new InvalidProjectData("Project short name cannot be empty or whitespace only");
        if (shortNameTrimmed.Length > 6) throw new InvalidProjectData("Project short name cannot be longer than 6 characters");
        if (!Regex.IsMatch(cpr.shortName, @"^[a-zA-Z\s]+$")) throw new InvalidProjectData("Project shortName can contain only letters and spaces");
       
        Project newProject = new Project(cpr.shortName, cpr.description);

        _db.Projects.Add(newProject);
        await _db.SaveChangesAsync();

        return newProject;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        l.LogDebug("Getting all projects from the db");
        List<Project> projects = await _db.Projects
            .Include(p => p.Issues)
            .Include(p => p.Keys)
            .ToListAsync();
        l.LogDebug($"Found {projects.Count} projects in the db");
        return projects;
    }

    public ProjectService(PostgresqlDbContext db, ILogger<ProjectService> l)
    {
        _db = db;
        this.l = l;
    }
}
