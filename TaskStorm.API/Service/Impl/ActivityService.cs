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

    public async Task<ActivityPropertyUpdated> CreateActivityPropertyUpdatedAsync(ActivityType Type, string OldValue, string NewValue, int issueId, int userId)
    {
        l.LogDebug($"Creating ActivityPropertyUpdated: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue, NewValue, issueId, Type, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;    
    }
    public async Task<ActivityPropertyCreated> CreateIssueAsync(ActivityType Type, int issueId, int authorId)
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

    public async Task<ActivityPropertyCreated> CreateCommenAsync(ActivityType Type, int issueId, int commentId)
    {
        l.LogDebug($"Creating Comment Activity: Type={Type}, issueId={issueId}, commentId={commentId}");
        var activity = new ActivityPropertyCreated(Type, issueId, commentId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateStatusAsync(ActivityType Type, IssueStatus OldValue, IssueStatus NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Status: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, Type, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;

    }

    public async Task<ActivityPropertyUpdated> UpdatePriorityAsync(ActivityType Type, IssuePriority OldValue, IssuePriority NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Priority: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, Type, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateAssigneeAsync(ActivityType Type, int OldValue, int NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Assignee: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, Type, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;

    }

    public async Task<ActivityPropertyUpdated> UpdateTeamAsync(ActivityType Type, int OldValue, int NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Team: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, Type, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateDueDateAsync(ActivityType Type, DateTime OldValue, DateTime NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating DueDate: Type={Type}, OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString("o"), NewValue.ToString("o"), issueId, Type, userId);
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
