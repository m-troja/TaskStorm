using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Request;
using TaskStorm.Service;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/project")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _ps;
    private readonly ProjectCnv _projectCnv;
    private readonly ILogger<ProjectController> _logger;

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProjectById(int id)
    {
        _logger.LogInformation("Called controller GetProjectById: {ProjectId}", id);
        var project = await _ps.GetProjectById(id);
        ProjectDto projectDto = _projectCnv.ConvertProjectToProjectDto(project);
        return Ok(projectDto);
    }

    [HttpDelete("id/{id:int}")]
    public async Task<ActionResult<string>> DeleteProjectById(int id)
    {
        _logger.LogInformation("Called controller DeleteProjectById with ID: {ProjectId}", id);

        await _ps.DeleteProjectById(id);
        return Ok($"Deleted project id={id}");
    }


    [HttpPost]
    [Route("create")]
    public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectRequest cpr)
    {
        _logger.LogInformation("Called controller createProject with ShortName: {ShortName}", cpr.shortName);

        var createdProject = await _ps.CreateProject(cpr);
        return Ok(_projectCnv.ConvertProjectToProjectDto(createdProject));
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<ProjectDto>>> GetAllProjects()
    {
        _logger.LogInformation("Called controller getAllProjects");

        var projects = await _ps.GetAllProjectsAsync();
        return Ok(_projectCnv.ConvertProjectsToProjectDtos(projects));
    }

    public ProjectController(IProjectService ps, ProjectCnv projectCnv, ILogger<ProjectController> logger)
    {
        _logger = logger;
        _ps = ps;
        _projectCnv = projectCnv;
    }
}
