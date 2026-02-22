using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [Authorize]
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

        var issueDto = _issueCnv.ConvertIssueToIssueDto(issue);
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

    [HttpPut("assign")]
    public async Task<ActionResult<IssueDto>> AssignIssue([FromBody] AssignIssueRequest req)
    {
        l.LogDebug($"Received assign issue request: {req}");
        var issue = await _is.AssignIssueAsync(req);
        return Ok(_issueCnv.ConvertIssueToIssueDto(issue));
    }

    [HttpPut("rename")]
    public async Task<ActionResult<IssueDto>> RenameIssue([FromBody] RenameIssueRequest req)
    {
        l.LogDebug($"Received rename issue request: {req.id}, {req.newTitle}");
        var issueDto = await _is.RenameIssueAsync(req);
        return Ok(issueDto);
    }

    [HttpPut("assign-team")]
    public async Task<ActionResult<IssueDto>> AssignTeam([FromBody] AssignTeamRequest req)
    {
        l.LogDebug($"Received AssignTeam request: {req.IssueId}, {req.TeamId}");
        var issueDto = await _is.AssignTeamAsync(req);
        return Ok(issueDto);
    }

    [HttpPut("update-status")]
    public async Task<ActionResult<IssueDto>> ChangeIssueStatus([FromBody] ChangeIssueStatusRequest req)
    {
        l.LogDebug($"Received update issue status request: {req.IssueId}, {req.NewStatus}");
        var issueDto = await _is.ChangeIssueStatusAsync(req);
        return Ok(issueDto);
    }

    [HttpPut("update-priority")]
    public async Task<ActionResult<IssueDto>> UpdateIssuePriority([FromBody] ChangeIssuePriorityRequest req)
    {
        l.LogDebug($"Received update issue priority request: {req.IssueId}, {req.NewPriority}");
        var issueDto = await _is.ChangeIssuePriorityAsync(req);
        return Ok(issueDto);
    }

    [HttpPut("update-due-date")]
    public async Task<ActionResult<IssueDto>> UpdateDueDate([FromBody] UpdateDueDateRequest req)
    {
        if (req is null || req.IssueId <= 0 || !req.DueDate.HasValue)
        {
            l.LogDebug($"Invalid UpdateDueDateRequest");
            throw new BadRequestException("Invalid request. IssueId must be positive and DueDate cannot be null.");
        }

        l.LogDebug($"Received update due date request: {req.IssueId}, {req.DueDate}");
        var issueDto = await _is.UpdateDueDateAsync(req);
        return Ok(issueDto);
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
        l.LogInformation($"Triggered endpoint DeletelIssueById {id}");
        await _is.DeleteIssueByIdAsync(id);
        return Ok($"Deleted issue {id}");
    }

    [HttpDelete("all")]
    public async Task<ActionResult<string>> DeleteAllIssues()
    {
        l.LogInformation("Triggered endpoint Delete all issues");
        await _is.deleteAllIssues();
        return Ok("All issues deleted successfully");
    }

    public IssueController(IIssueService @is, ILogger<IssueController> l, IssueCnv _issueCnv)
    {
        _is = @is;
        this.l = l;
        this._issueCnv = _issueCnv;
    }
}
