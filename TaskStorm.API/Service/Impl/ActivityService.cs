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

    public async Task<ActivityPropertyCreated> CreateIssueAsync(int issueId, int authorId)
    {
        l.LogDebug($"Creating ActivityPropertyCreated: issueId={issueId}, authorId={authorId}");
        var activity = new ActivityPropertyCreated(ActivityType.CREATED_ISSUE, issueId, authorId);
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

    public async Task<ActivityPropertyCreated> CreateCommenAsync(int issueId, int commentId, int EventAuthorId)
    {
        l.LogDebug($"Creating Comment Activity:issueId={issueId}, commentId={commentId}");
        var activity = new ActivityPropertyCreated(ActivityType.CREATED_COMMENT, issueId, EventAuthorId) { CommentId = commentId};
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateStatusAsync(IssueStatus OldValue, IssueStatus NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Status: OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, ActivityType.UPDATED_STATUS, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;

    }

    public async Task<ActivityPropertyUpdated> UpdatePriorityAsync(IssuePriority OldValue, IssuePriority NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Priority: OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, ActivityType.UPDATED_PRIORITY, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateAssigneeAsync(int OldValue, int NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Assignee: OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, ActivityType.UPDATED_ASSIGNEE, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;

    }

    public async Task<ActivityPropertyUpdated> UpdateTeamAsync(int OldValue, int NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating Team: OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString(), NewValue.ToString(), issueId, ActivityType.UPDATED_TEAM, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateDueDateAsync(DateTime OldValue, DateTime NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating DueDate: OldValue={OldValue}, NewValue={NewValue}, issueId={issueId}, userId={userId}");
        var activity = new ActivityPropertyUpdated(OldValue.ToString("o"), NewValue.ToString("o"), issueId, ActivityType.UPDATED_DUEDATE, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateDescriptionAsync(string OldValue, string NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating description {OldValue} to {NewValue} ");
        var activity = new ActivityPropertyUpdated(OldValue, NewValue, issueId, ActivityType.UPDATED_DESCRIPTION, userId);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<ActivityPropertyUpdated> UpdateTitleAsync(string OldValue, string NewValue, int issueId, int userId)
    {
        l.LogDebug($"Updating title {OldValue} to {NewValue} ");
        var activity = new ActivityPropertyUpdated(OldValue, NewValue, issueId, ActivityType.UPDATED_TITLE, userId);
        await _db.Activities.AddAsync(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public ActivityService(PostgresqlDbContext db, ILogger<ActivityService> l)
    {
        _db = db;
        this.l = l;
    }

}
