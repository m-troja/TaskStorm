using Serilog.Core;
using System.Net.Http.Json;
using TaskStorm.Exception.GptException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.ChatGpt;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Tools;
namespace TaskStorm.Service.Impl;

public class SlackNotificationService : ISlackNotificationService
{
    private readonly IssueCnv _issueCnv;
    private readonly ILogger<SlackNotificationService> logger;
    private readonly string _EventEndpoint = "api/v1/taskstorm/event";
    private readonly String _ChatServerAddress = Environment.GetEnvironmentVariable("CHAT_SERVER_ADDRESS") ?? "localhost";
    private readonly String _ChatServerPort = Environment.GetEnvironmentVariable("CHAT_SERVER_PORT") ?? "6969";
    private readonly HttpClient _httpClient;
    private readonly string _ChatServerUri;

    public SlackNotificationService(
        ILogger<SlackNotificationService> logger,
        IssueCnv issueCnv,
        HttpClient httpClient)
    {
        this._httpClient = httpClient;
        this.logger = logger;
        this._issueCnv = issueCnv;

        var address = Environment.GetEnvironmentVariable("CHAT_SERVER_ADDRESS") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("CHAT_SERVER_PORT") ?? "6969";
        logger.LogDebug("Chat server address: {address}, port: {port}", address, port);
        _ChatServerUri = $"http://{address}:{port}/{_EventEndpoint}";
        logger.LogDebug("Chat server URI set to {uri}", _ChatServerUri);
    }
    public async Task SendIssueCreatedNotificationAsync(Issue issue)
    {
        logger.LogDebug("Preparing to send issue created notification for issue {issueId} to ChatGPT", issue.Id);
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ChatGptEvent.ISSUE_CREATED, issueDto);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssueAssignedNotificationAsync(Issue issue)
    {
        logger.LogDebug("Preparing to send issue assigned notification for issue {issueId} to ChatGPT", issue.Id);
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ChatGptEvent.ISSUE_ASSIGNED, issueDto);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssueDueDateUpdatedNotificationAsync(Issue issue)
    {
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ChatGptEvent.UPDATE_DUEDATE, issueDto);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssuePriorityChangedNotificationAsync(Issue issue)
    {
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ChatGptEvent.UPDATE_PRIORITY, issueDto);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssueStatusChangedNotificationAsync(Issue issue)
    {
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ChatGptEvent.UPDATE_STATUS, issueDto);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendCommentAddedNotificationAsync(Issue issue)
    {
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ChatGptEvent.COMMENT_CREATED, issueDto);
        await sendEventToChatGpt(chatEvent);
    }

    private async Task sendEventToChatGpt(ChatGptDto chatEvent)
    {
        try
        {
            logger.LogInformation("Sending event to ChatGPT at {uri}", _ChatServerUri);

            var response = await _httpClient.PostAsJsonAsync(
                _ChatServerUri,
                chatEvent,
                JsonOptions.Default);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Chat server returned {StatusCode}", response.StatusCode);
            }
            else
            {
                logger.LogInformation("Event {event} sent successfully", chatEvent.Event);
            }
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error while sending event to ChatGPT");
            //throw new GptConnectionException("Error connecting to ChatGPT server"); 
        }
    }
}
