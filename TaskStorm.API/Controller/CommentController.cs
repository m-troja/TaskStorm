using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprache;
using TaskStorm.Model.DTO;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Service;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/comment")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _cs;
    private readonly ILogger<CommentController> logger;

    [HttpPost]
    [Route("create")]   
    public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentRequest cmr)
    {
        var dto = await _cs.CreateCommentAsync(cmr);
        return Ok(dto);
    }

    [HttpGet]
    [Route("issue/{issueId:int}")]
    public async Task<ActionResult<List<CommentDto>>> GetCommentsByIssueId(int issueId)
    {
        logger.LogInformation($"Received GetCommentsByIssueId {issueId}");

        var comments = await _cs.GetCommentsByIssueIdAsync(issueId);

        return Ok(comments.ToList());
    }

    [HttpDelete]
    [Route("issue/{issueId:int}")]
    public async Task<ActionResult<string>> DeleteAllCommentsByIssueId(int issueId)
    {
        await _cs.DeleteAllCommentsByIssueId(issueId);
        return Ok($"Deleted all comments for issue id={issueId}");
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<ActionResult<string>> DeleteCommentById(int id)
    {
        await _cs.DeleteCommentById(id);
        return Ok($"Deleted comment by id={id}");
    }

    public CommentController(ICommentService cs, ILogger<CommentController> logger)
    {
        _cs = cs;
        this.logger = logger;
    }
    
    [HttpPut]
    [Route("edit")]
    public async Task<ActionResult<CommentDto>> EditComment([FromBody] EditCommentRequest req)
    {
        var dto = await _cs.EditCommentAsync(req);
        return Ok(dto);
    }
}
