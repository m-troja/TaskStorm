using TaskStorm.Model.Event;
using TaskStorm.Service;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System.Text.Json;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.EventBus.Handler;

public class IssueEventHandler : IHostedService
{
    private readonly IActivityService _activityService;
    private readonly ISlackNotificationService _slackNotificationService;
    private IConnection _connection;

    public IssueEventHandler(IActivityService activityService,
                             ISlackNotificationService slackNotificationService)
    {
        _activityService = activityService;
        _slackNotificationService = slackNotificationService;

        var factory = new ConnectionFactory("activemq:tcp://localhost:61616");
        _connection = factory.CreateConnection();
        _connection.Start();

        StartListener();
    }

    private void StartListener()
    {
        var session = _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        IDestination destination = session.GetTopic("taskstorm.events");
        using var consumer = session.CreateConsumer(destination);

        consumer.Listener += async message =>
        {
            if (message is ITextMessage textMessage)
            {
                try
                {
                    var ev = JsonSerializer.Deserialize<IssueChangedEvent>(textMessage.Text);
                    if (ev != null)
                        await HandleEvent(ev);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"Error processing event: {ex}");
                }
            }
        };
    }

    private async Task HandleEvent(IssueChangedEvent ev)
    {
        switch (ev.FieldChanged)
        {
            case "Title":
                await _activityService.UpdateTitleAsync(ev.OldValue ?? "", ev.NewValue ?? "", ev.IssueId, ev.AuthorId);
                var issueForTitle = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendUpdateTitleAsync(issueForTitle, new User { Id = ev.AuthorId });
                break;

            case "Description":
                await _activityService.UpdateDescriptionAsync(ev.OldValue ?? "", ev.NewValue ?? "", ev.IssueId, ev.AuthorId);
                var issueForDesc = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendUpdateDescriptionAsync(issueForDesc, new User { Id = ev.AuthorId });
                break;

            case "Status":
                if (Enum.TryParse<IssueStatus>(ev.OldValue, out var oldStatus) &&
                    Enum.TryParse<IssueStatus>(ev.NewValue, out var newStatus))
                {
                    await _activityService.UpdateStatusAsync(oldStatus, newStatus, ev.IssueId, ev.AuthorId);
                    var issueForStatus = new Issue { Id = ev.IssueId };
                    await _slackNotificationService.SendIssueStatusChangedNotificationAsync(issueForStatus, new User { Id = ev.AuthorId });
                }
                break;

            case "Priority":
                if (Enum.TryParse<IssuePriority>(ev.OldValue, out var oldPriority) &&
                    Enum.TryParse<IssuePriority>(ev.NewValue, out var newPriority))
                {
                    await _activityService.UpdatePriorityAsync(oldPriority, newPriority, ev.IssueId, ev.AuthorId);
                    var issueForPriority = new Issue { Id = ev.IssueId };
                    await _slackNotificationService.SendIssuePriorityChangedNotificationAsync(issueForPriority, new User { Id = ev.AuthorId });
                }
                break;

            case "Assignee":
                await _activityService.UpdateAssigneeAsync(int.Parse(ev.OldValue ?? "-1"), int.Parse(ev.NewValue ?? "-1"), ev.IssueId, ev.AuthorId);
                var issueForAssignee = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendIssueAssignedNotificationAsync(issueForAssignee, new User { Id = ev.AuthorId });
                break;

            case "Team":
                await _activityService.UpdateTeamAsync(int.Parse(ev.OldValue ?? "-1"), int.Parse(ev.NewValue ?? "-1"), ev.IssueId, ev.AuthorId);
                var issueForTeam = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendTeamAssignedNotificationAsync(issueForTeam, new User { Id = ev.AuthorId });
                break;

            case "DueDate":
                await _activityService.UpdateDueDateAsync(DateTime.Parse(ev.OldValue ?? DateTime.MinValue.ToString()),
                                                           DateTime.Parse(ev.NewValue ?? DateTime.MinValue.ToString()),
                                                           ev.IssueId, ev.AuthorId);
                var issueForDue = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendIssueDueDateUpdatedNotificationAsync(issueForDue, new User { Id = ev.AuthorId });
                break;

            case "Comment":
                if (ev.CommentId.HasValue)
                {
                    await _activityService.CreateCommenAsync(ev.IssueId, ev.CommentId.Value, ev.AuthorId);
                    var issueForComment = new Issue { Id = ev.IssueId };
                    await _slackNotificationService.SendCommentAddedNotificationAsync(issueForComment, new User { Id = ev.AuthorId });
                }
                break;

            case "Created":
                await _activityService.CreateIssueAsync(ev.IssueId, ev.AuthorId);
                var issueForCreated = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendIssueCreatedNotificationAsync(issueForCreated, new User { Id = ev.AuthorId });
                break;

            case "Deleted":
                await _activityService.DeleteActivitiesForIssueId(ev.IssueId);
                var issueForDeleted = new Issue { Id = ev.IssueId };
                await _slackNotificationService.SendIssueDeletedNotificationAsync(issueForDeleted, new User { Id = ev.AuthorId });
                break;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory("activemq:tcp://localhost:61616");
        _connection = factory.CreateConnection();
        _connection.Start();

        var session = _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        IDestination destination = session.GetTopic("taskstorm.events");
        var consumer = session.CreateConsumer(destination);

        consumer.Listener += async message =>
        {
            if (message is ITextMessage textMessage)
            {
                try
                {
                    var ev = JsonSerializer.Deserialize<IssueChangedEvent>(textMessage.Text);
                    if (ev != null)
                        await HandleEvent(ev);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"Error processing event: {ex}");
                }
            }
        };

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _connection?.Close();
        return Task.CompletedTask;
    }

}