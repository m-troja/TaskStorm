using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

public interface IActivityService
{

    public Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId, int userId);
    public Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(ActivityType Type, int issueId);

    Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(
        ActivityType Type,
        int issueId,
        int authorId);

    Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId);

    Task DeleteActivitiesForIssueId(int id);
}