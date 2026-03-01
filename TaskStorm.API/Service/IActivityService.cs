using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Service;

public interface IActivityService
{
    Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId);
    Task DeleteActivitiesForIssueId(int id);
    public Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId, int userId);
    public Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(ActivityType Type, int issueId);

}
