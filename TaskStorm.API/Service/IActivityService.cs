using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Service;

public interface IActivityService
{
    Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId);
    Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(ActivityType Type, int propertyId, int authorId);
    Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId);
    Task DeleteActivitiesForIssueId(int id);

}
