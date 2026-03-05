using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskStorm.Data;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Service.Impl;
using Xunit;

public class ActivityServiceTests
{
    private PostgresqlDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PostgresqlDbContext(options);
    }

    private ActivityService CreateService(PostgresqlDbContext db)
    {
        var logger = Mock.Of<ILogger<ActivityService>>();
        return new ActivityService(db, logger);
    }

    [Fact]
    public async Task CreateIssueAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.CreateIssueAsync(1, 10);

        Assert.NotNull(activity);
        Assert.Equal(ActivityType.CREATED_ISSUE, activity.Type);
        Assert.Equal(1, activity.IssueId);
        Assert.Equal(10, activity.EventAuthorUserId);

        var dbActivity = await db.Activities.FirstOrDefaultAsync();
        Assert.NotNull(dbActivity);
        Assert.Equal(ActivityType.CREATED_ISSUE, dbActivity.Type);
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldAddCommentActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.CreateCommenAsync(1, 100, 10);

        Assert.NotNull(activity);
        Assert.Equal(ActivityType.CREATED_COMMENT, activity.Type);
        Assert.Equal(1, activity.IssueId);
        Assert.Equal(100, activity.CommentId);
        Assert.Equal(10, activity.EventAuthorUserId);

        var dbActivity = await db.Activities.FirstOrDefaultAsync();
        Assert.NotNull(dbActivity);
        Assert.Equal(100, dbActivity.CommentId);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.UpdateStatusAsync(IssueStatus.NEW, IssueStatus.IN_PROGRESS, 1, 10);

        Assert.NotNull(activity);
        Assert.Equal(ActivityType.UPDATED_STATUS, activity.Type);
        Assert.Equal(1, activity.IssueId);
        Assert.Equal(10, activity.EventAuthorUserId);
        Assert.Equal("NEW", activity.OldValue);
        Assert.Equal("IN_PROGRESS", activity.NewValue);
    }

    [Fact]
    public async Task GetActivitiesByIssueIdAsync_ShouldReturnList()
    {
        var db = GetDb();
        var service = CreateService(db);

        // Seed two activities
        await service.CreateIssueAsync(1, 10);
        await service.CreateCommenAsync(1, 100, 10);

        var list = await service.GetActivitiesByIssueIdAsync(1);

        Assert.Equal(2, list.Count);
        Assert.All(list, a => Assert.Equal(1, a.IssueId));
    }

    [Fact]
    public async Task DeleteActivitiesForIssueId_ShouldRemoveActivities()
    {
        var db = GetDb();
        var service = CreateService(db);

        await service.CreateIssueAsync(1, 10);
        await service.CreateCommenAsync(1, 100, 10);

        await service.DeleteActivitiesForIssueId(1);

        var list = await db.Activities.Where(a => a.IssueId == 1).ToListAsync();
        Assert.Empty(list);
    }

    [Fact]
    public async Task UpdatePriorityAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.UpdatePriorityAsync(IssuePriority.NORMAL, IssuePriority.HIGH, 1, 10);

        Assert.Equal(ActivityType.UPDATED_PRIORITY, activity.Type);
        Assert.Equal("NORMAL", activity.OldValue);
        Assert.Equal("HIGH", activity.NewValue);
    }

    [Fact]
    public async Task UpdateAssigneeAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.UpdateAssigneeAsync(1, 2, 1, 10);

        Assert.Equal(ActivityType.UPDATED_ASSIGNEE, activity.Type);
        Assert.Equal("1", activity.OldValue);
        Assert.Equal("2", activity.NewValue);
    }

    [Fact]
    public async Task UpdateTeamAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.UpdateTeamAsync(1, 2, 1, 10);

        Assert.Equal(ActivityType.UPDATED_TEAM, activity.Type);
        Assert.Equal("1", activity.OldValue);
        Assert.Equal("2", activity.NewValue);
    }

    [Fact]
    public async Task UpdateDueDateAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var oldDate = new DateTime(2026, 3, 5);
        var newDate = new DateTime(2026, 3, 10);

        var activity = await service.UpdateDueDateAsync(oldDate, newDate, 1, 10);

        Assert.Equal(ActivityType.UPDATED_DUEDATE, activity.Type);
        Assert.Equal(oldDate.ToString("o"), activity.OldValue);
        Assert.Equal(newDate.ToString("o"), activity.NewValue);
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.UpdateDescriptionAsync("Old desc", "New desc", 1, 10);

        Assert.Equal(ActivityType.UPDATED_DESCRIPTION, activity.Type);
        Assert.Equal("Old desc", activity.OldValue);
        Assert.Equal("New desc", activity.NewValue);
    }

    [Fact]
    public async Task UpdateTitleAsync_ShouldAddActivity()
    {
        var db = GetDb();
        var service = CreateService(db);

        var activity = await service.UpdateTitleAsync("Old title", "New title", 1, 10);

        Assert.Equal(ActivityType.UPDATED_TITLE, activity.Type);
        Assert.Equal("Old title", activity.OldValue);
        Assert.Equal("New title", activity.NewValue);
    }
}