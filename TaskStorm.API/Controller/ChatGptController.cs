using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Request;
using TaskStorm.Service;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/chatgpt")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ChatGptController : ControllerBase
{
    private readonly ILogger<ChatGptController> l;
    private readonly IUserService _userService;
    private readonly IIssueService _iss;
    private readonly IssueCnv _issueCnv;

    [HttpGet("user/slack-user-id/{slackUserID}")]
    public async Task<UserDto> GetUserBySlackUserId(String slackUserId)
    {
        l.LogInformation($"Received get user by Slack user ID request: {slackUserId}");
        var userDto = await _userService.GetUserBySlackUserIdAsync(slackUserId);
        l.LogDebug($"Returning user DTO: {userDto}");
        return userDto;
    }

    [HttpPost("issue/create")]
    public async Task<IssueDtoChatGpt> CreateIssueBySlack([FromBody] SlackCreateIssueRequest req)
    {
        l.LogInformation($"Received create issue by Slack request: {req}");
        var issueDto = await _iss.CreateIssueBySlackAsync(req);
        return issueDto;
    }
    [HttpPut("issue/assign")]
    public async Task<IssueDtoChatGpt> AssignIssueByChatGpt([FromBody] AssignIssueRequestChatGpt req)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        l.LogInformation($"Received AssignIssueBySlack request: {req}, userId={userId}");
        var issue = await _iss.AssignIssueBySlackAsync(req, userId);
        return _issueCnv.EntityToIssueDtoChatGpt(issue);
    }

    [HttpGet("issues/slack-user-id/{slackUserId}")]
    public async Task<ActionResult<List<IssueDtoChatGpt>>> GetIssuesBySlackUserId(string slackUserId)
    {
        l.LogInformation($"Received GetIssuesBySlackUserId: {slackUserId}");
        var issues = await _iss.GetIssuesBySlackUserId(slackUserId);
        var dtos = _issueCnv.EntityListToChatGptDtoList(issues);

        return Ok(dtos);
    }


    public ChatGptController(IUserService userService, ILogger<ChatGptController> logger, IIssueService iss, IssueCnv issueCnv)
    {
        _userService = userService;
        l = logger;
        _iss = iss;
        _issueCnv = issueCnv;
    }
}
