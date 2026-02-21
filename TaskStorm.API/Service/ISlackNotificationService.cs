using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Service;
    public interface ISlackNotificationService
{
        Task SendIssueCreatedNotificationAsync(Issue issue);
        Task SendIssueAssignedNotificationAsync(Issue issue);
        Task SendIssueStatusChangedNotificationAsync(Issue issue);
        Task SendIssuePriorityChangedNotificationAsync(Issue issue);
        Task SendIssueDueDateUpdatedNotificationAsync(Issue issue);
        Task SendCommentAddedNotificationAsync(Issue issue);
}
