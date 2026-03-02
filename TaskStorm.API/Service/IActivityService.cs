using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Service;

public interface IActivityService
{
    Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId);
    Task DeleteActivitiesForIssueId(int id);
    Task<ActivityPropertyCreated> CreateIssueAsync(ActivityType Type, int issueId, int creatorUserId);
    Task<ActivityPropertyCreated> CreateCommenAsync(ActivityType Type, int issueId, int commentId);
    Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateStatusAsync(ActivityType Type, IssueStatus OldValue, IssueStatus NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdatePriorityAsync(ActivityType Type, IssuePriority OldValue, IssuePriority NewValue, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateAssigneeAsync(ActivityType Type, int OldAssigneeId, int NewAssigneeId, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateTeamAsync(ActivityType Type, int OldTeamId, int NewTeamId, int issueId, int userId);
    Task<ActivityPropertyUpdated> UpdateDueDateAsync(ActivityType Type, DateTime OldValue, DateTime NewValue, int issueId, int userId);

}
