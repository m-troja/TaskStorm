using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskStorm.Exception;
using TaskStorm.Exception.IssueException;
using TaskStorm.Log;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Service;
using TaskStorm.Service.Impl;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/issue")]
public class IssueController : ControllerBase
{
    private readonly IIssueService _is;
    private readonly ILogger<IssueController> l;
    private readonly IssueCnv _issueCnv;
    private readonly ActivityCnv _activityCnv;
    private readonly IActivityService _activityService;

    [HttpPost]
    [Route("create")]
    public async Task<ActionResult<IssueDto>> CreateIssue(CreateIssueRequest req)
    {
        l.LogDebug($"Received create issue request: {req}");
        Issue issue;
        try 
        {
            issue = await _is.CreateIssueAsync(req);
        }
        catch (IssueCreationException)
        {

            throw;
        }

        var issueDto = _issueCnv.EntityToDto(issue);
        return Ok(issueDto);
    }

    [HttpGet("key/{key}")]
    public async Task<ActionResult<IssueDto>> GetIssueByKey(string key)
    {
        if (key == null || key == string.Empty || key.Length == 0)
        {
            l.LogDebug($"issue.key is null");
            throw new BadRequestException("key cannot be empty");
        }

        l.LogDebug($"Received get issue by key request: {key}");
        var IssueDto = await _is.GetIssueDtoByKeyAsync(key);

        return Ok(IssueDto);

    }

    [HttpGet("all")]
    public async Task<ActionResult<List<IssueDto>>> GetAllIssues()
    {
        l.LogDebug($"Received get all issues request");
        var issues = await _is.GetAllIssues();
        return Ok(issues);

    }
    [HttpGet("team/{id:int}")]
    public async Task<ActionResult<List<IssueDto>>> GetIssuesByTeamId(int id)
    {
        l.LogDebug("Received GetIssuesByTeamId: {id}", id);
        var issues = await _is.GetIssuesByTeamId(id);
        return Ok(issues);

    }
    [HttpGet("id/{id:int}")]
    public async Task<ActionResult<IssueDto>> GetIssueById(int id)
    {
        l.LogDebug($"Received get issue by id request: {id}");
        var IssueDto = await _is.GetIssueDtoByIdAsync(id);
       
        return Ok(IssueDto);
    }

    [HttpPut("update")]
    public async Task<ActionResult<IssueDto>> UpdateIssue([FromBody] UpdateIssueRequest req)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        l.LogDebug($"Received UpdateIssue request: {req}");
        var issue = await _is.HandleUpdateIssueRequestAsync(req, userId);
        return Ok(_issueCnv.EntityToDto(issue));
    }


    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<IssueDto>>> GetAllIssuesByUserId(int userId)
    {
        l.LogDebug($"Received get all issues by user id request: {userId}");
        var issuesDto = await _is.GetIssuesByUserId(userId);
        return Ok(issuesDto);
    }

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<List<IssueDto>>> GetAllIssuesByProjectId(int projectId)
    {
        l.LogDebug($"Received get all issues by project id request: {projectId}");
        var issuesDto = await _is.GetIssuesByProjectId(projectId);
        return Ok(issuesDto);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<string>> DeletelIssueById(int id)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        l.LogInformation($"Triggered endpoint DeletelIssueById {id}");
        try
        {
            await _is.DeleteIssueByIdAsync(id, userId);

        }
        catch (IssueNotFoundException)
        {
            l.LogWarning($"DeletelIssueById: Issue with id {id} not found for deletion");
            throw;
        }
        catch (BadRequestException)
        {
            l.LogWarning($"DeletelIssueById: author == null - skip deleting issue");
            throw;
        }
        return Ok($"Deleted issue {id}");
    }

    [HttpDelete("all")]
    public async Task<ActionResult<string>> DeleteAllIssues()
    {
        l.LogInformation("Triggered endpoint Delete all issues");
        await _is.deleteAllIssues();
        return Ok("All issues deleted successfully");
    }

    [HttpGet("{id:int}/activities")]
    public async Task<ActionResult<List<ActivityDto>>> GetActivitiesByIssueId(int id)
    {
        l.LogDebug($"Received get activities by issue id request: {id}");
        var activities = await _activityService.GetActivitiesByIssueIdAsync(id);
        var dtos = _activityCnv.EntityListToDtoList(activities);
        return Ok(dtos);
    }

    public IssueController(IIssueService @is, ILogger<IssueController> l, IssueCnv _issueCnv, IActivityService _activityService, ActivityCnv activityCnv)
    {
        this._activityService = _activityService;
        _is = @is;
        this.l = l;
        this._issueCnv = _issueCnv;
        _activityCnv = activityCnv;
    }
}
