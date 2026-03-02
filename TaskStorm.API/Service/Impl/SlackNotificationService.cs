using Serilog.Core;
using System.Net.Http.Json;
using TaskStorm.Exception.GptException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.ChatGpt;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
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
    public async Task SendIssueCreatedNotificationAsync(Issue issue, User author)
    {
        logger.LogInformation("Preparing to send issue created notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.CREATED_ISSUE, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssueAssignedNotificationAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to send issue assigned notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_ASSIGNEE, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssueDueDateUpdatedNotificationAsync(Issue issue, User author)
    {
            logger.LogDebug("Preparing to send issue due date updated notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_DUEDATE, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssuePriorityChangedNotificationAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to send issue priority changed notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_PRIORITY, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendIssueStatusChangedNotificationAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to send issue status changed notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_STATUS, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    public async Task SendCommentAddedNotificationAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to send comment added notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.CREATED_COMMENT, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }
    public async Task SendIssueDeletedNotificationAsync(Issue issue, User author)
    {
        issue.Author = author;
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.DELETED_ISSUE, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }
    public async Task SendTeamAssignedNotificationAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to send team assigned notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_TEAM, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    private async Task sendEventToChatGpt(ChatGptDto chatEvent)
    {
        try
        {
            logger.LogInformation("Sending event to ChatGPT at {uri}", _ChatServerUri);
            logger.LogInformation("Sending ChatGptDto: {event}", chatEvent);

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

    public async Task SendUpdateDescriptionAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to SendUpdateDescription notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_DESCRIPTION, issueDto, author.SlackUserId);
        await sendEventToChatGpt(chatEvent);
    }

    public Task SendUpdateTitleAsync(Issue issue, User author)
    {
        logger.LogDebug("Preparing to SendUpdateTitle notification for issue {issueId} to ChatGPT by slackUserId {slack}", issue.Id, author.SlackUserId);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        var chatEvent = new ChatGptDto(ActivityType.UPDATED_TITLE, issueDto, author.SlackUserId);
        return sendEventToChatGpt(chatEvent);
    }
}
