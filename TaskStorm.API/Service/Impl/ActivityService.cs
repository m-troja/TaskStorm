using TaskStorm.Data;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Log;
using Microsoft.EntityFrameworkCore;
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
    public async Task<ActivityPropertyCreated> CreateActivityPropertyCreatedAsync(ActivityType Type, int issueId, int authorId)
    {
        l.LogDebug($"Creating ActivityPropertyCreated: Type={Type}, issueId={issueId}, authorId={authorId}");
        var activity = new ActivityPropertyCreated(Type, issueId, authorId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }
    public async Task<List<Activity>> GetActivitiesByIssueIdAsync(int issueId)
    {
       l.LogDebug($"Getting activities for issueId={issueId}");
        var activities = await _db.Activities.Where(a => a.IssueId == issueId).ToListAsync();

        l.LogDebug($"Found {activities.Count} activities for issueId={issueId}");
        return activities;
    }
    public async Task DeleteActivitiesForIssueId(int id)
    {
        _db.Activities.RemoveRange(_db.Activities.Where(a => a.IssueId == id));
        l.LogDebug($"Deleted activities for issueId={id}");
        await _db.SaveChangesAsync();

    }

    public ActivityService(PostgresqlDbContext db, ILogger<ActivityService> l)
    {
        _db = db;
        this.l = l;
    }
}
