using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Service;

public interface IActivityService
{
    public Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId);
    public Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(ActivityType Type, int issueId);

}
