using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

public interface IActivityService
{
    Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId);
    Task DeleteActivitiesForIssueId(int id);
    Task<ActivityPropertyCreated> CreateIssueAsync(int issueId, int creatorUserId);
    Task<ActivityPropertyCreated> CreateCommenAsync(int issueId, int commentId, int EventAuthorId);
    Task<ActivityPropertyUpdated> UpdateStatusAsync(IssueStatus OldValue, IssueStatus NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdatePriorityAsync(IssuePriority OldValue, IssuePriority NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateAssigneeAsync(int OldAssigneeId, int NewAssigneeId, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateTeamAsync(int OldTeamId, int NewTeamId, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateDueDateAsync(DateTime OldValue, DateTime NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateDescriptionAsync(string OldValue, string NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateTitleAsync(string OldValue, string NewValue, int issueId, int userId);

    Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(
        ActivityType Type,
        int issueId,
        int authorId);

    Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId);

    Task DeleteActivitiesForIssueId(int id);
}