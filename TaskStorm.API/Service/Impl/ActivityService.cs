using TaskStorm.Data;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Log;
namespace TaskStorm.Service.Impl;

public class ActivityService : IActivityService
{
    private readonly PostgresqlDbContext _db;
    private readonly ILogger<ActivityService> l;

    public async Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId)
    {
        l.LogDebug($"Creating ActivityPropertyUpdated: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}");
        var activity = new ActivityPropertyUpdated(OldValue, NewValue, issueId, Type);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;    
    }
    public async Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(ActivityType Type, int issueId)
    {
        l.LogDebug($"Creating ActivityPropertyCreated: Type={Type}, issueId={issueId}");
        var activity = new ActivityPropertyCreated(Type, issueId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public ActivityService(PostgresqlDbContext db, ILogger<ActivityService> l)
    {
        _db = db;
        this.l = l;
    }
}
