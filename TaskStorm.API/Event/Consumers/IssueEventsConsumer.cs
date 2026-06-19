using MassTransit;
using Microsoft.AspNetCore.SignalR;
using TaskStorm.Data;
using TaskStorm.Event.Hubs;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskStorm.Event.Consumers;

public class IssueEventsConsumer :
    IConsumer<IssueCreatedEvent>,
    IConsumer<IssueAssignedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly PostgresqlDbContext _db;
    private readonly IActivityService _activityService;
    private readonly ISlackNotificationService _slackNotificationService;
    private readonly ILogger<IssueEventsConsumer> l;


    public IssueEventsConsumer(IHubContext<NotificationHub> hubContext, PostgresqlDbContext db, IActivityService activityService, ISlackNotificationService slackNotificationService, ILogger<IssueEventsConsumer> l)
    {
        _hubContext = hubContext;
        _db = db;
        _activityService = activityService;
        _slackNotificationService = slackNotificationService;
        this.l = l;
    }

    public async Task Consume(ConsumeContext<IssueCreatedEvent> ev)
    {
        var issue = ev.Message.Issue;
        var author = issue.Author;
        l.LogInformation("IssueEventsConsumer: Consume IssueCreatedEvent for Issue ID: {IssueId} by Author ID: {AuthorId}", issue.Id, author.Id);

        var activity = await _activityService.CreateIssueAsync(issue.Id, author.Id);
        await _slackNotificationService.SendIssueCreatedNotificationAsync(issue, author);
    }

    public async Task Consume(ConsumeContext<IssueAssignedEvent> ev)
    {
        var msg = ev.Message;

        l.LogDebug("IssueEventsConsumer: Consume IssueAssignedEvent for Issue ID: {IssueId}. Old Assignee ID: {OldAssigneeId}, New Assignee ID: {NewAssigneeId}",
            msg.IssueId, msg.OldAssigneeId, msg.NewAssigneeId);

        //await _slackNotificationService.SendIssueAssignedNotificationAsync(msg.IssueId, msg.NewAssigneeId);

        await _activityService.UpdateAssigneeAsync(msg.OldAssigneeId, msg.NewAssigneeId, msg.IssueId, msg.EventAuthorId);

        var userIdsToNotify = new List<int> { msg.OldAssigneeId, msg.NewAssigneeId }.Distinct();

        foreach (var userId in userIdsToNotify)
        {
            var notification = new Notification
            {
                UserId = userId,
                EventAuthorId = msg.EventAuthorId,
                IssueId = msg.IssueId,
                Key = msg.IssueKey,
                Type = EventType.ISSUE_ASSIGNED,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Properties = new Dictionary<string, string>
            {
                { "OldAssigneeId", msg.OldAssigneeId.ToString() },
                { "NewAssigneeId", msg.NewAssigneeId.ToString() },
                { "OldAssigneeName", msg.OldAssigneeName },
                { "NewAssigneeName", msg.NewAssigneeName },
                { "IssueTitle", msg.IssueTitle },
                { "AuthorName", msg.EventAuthorName }
            }
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }

    }
}