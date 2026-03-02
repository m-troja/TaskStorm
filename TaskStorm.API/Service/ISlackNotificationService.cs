using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Service;
    public interface ISlackNotificationService
{
        Task SendIssueCreatedNotificationAsync(Issue issue, User author);
        Task SendIssueAssignedNotificationAsync(Issue issue, User author);
        Task SendIssueStatusChangedNotificationAsync(Issue issue, User author);
        Task SendIssuePriorityChangedNotificationAsync(Issue issue, User author);
        Task SendIssueDueDateUpdatedNotificationAsync(Issue issue, User author);
        Task SendCommentAddedNotificationAsync(Issue issue, User author);
        Task SendIssueDeletedNotificationAsync(Issue issue, User author);
        Task SendTeamAssignedNotificationAsync(Issue issue, User author);
        Task SendUpdateDescriptionAsync(Issue issue, User author);
        Task SendUpdateTitleAsync(Issue issue, User author);
}
