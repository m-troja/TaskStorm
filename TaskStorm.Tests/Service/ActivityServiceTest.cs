using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStorm.Data;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class ActivityServiceTests
{
    private static PostgresqlDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new PostgresqlDbContext(options);

        db.Activities.RemoveRange(db.Activities);
        db.SaveChanges();

        return db;
    }

    private static ActivityService CreateService(PostgresqlDbContext db)
    {
        var logger = new Mock<ILogger<ActivityService>>();
        return new ActivityService(db, logger.Object);
    }

    [Fact]
    public async Task CreateActivityPropertyCreatedAsync_ShouldCreateAndPersistActivity()
    {
        // Arrange
        var db = GetInMemoryDb();
        var mockLogger = new Mock<ILogger<ActivityService>>();
        var service = new ActivityService(db, mockLogger.Object);

        var type = ActivityType.CREATED_COMMENT;
        var issueId = 42;

        // Act
        string oldValue = "Old Value";
        string newValue = "New Value";
        var activity = await service.CreateActivityPropertyUpdatedAsync(type, oldValue, newValue, issueId);

        // Assert
        Assert.NotNull(activity);
        Assert.IsType<ActivityPropertyUpdated>(activity);
        Assert.Equal(oldValue, activity.OldValue);
        Assert.Equal(newValue, activity.NewValue);
        Assert.Equal(type, activity.Type);

        var dbActivity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(dbActivity);
    }


    [Fact]
    public async Task CreateActivityPropertyUpdatedAsync_ShouldCreateAndPersistActivity()
    {
        // arrange
        await using var db = GetInMemoryDb();
        var service = CreateService(db);

        var type = ActivityType.UPDATED_STATUS;
        var oldValue = "OPEN";
        var newValue = "CLOSED";
        var issueId = 99;

        // act
        var result = await service.CreateActivityPropertyUpdatedAsync(type, oldValue, newValue, issueId);

        // assert
        Assert.Equal(type, result.Type);
        Assert.Equal(oldValue, result.OldValue);
        Assert.Equal(newValue, result.NewValue);
        Assert.Equal(issueId, result.IssueId);

        var fromDb = await db.Activities
            .OfType<ActivityPropertyUpdated>()
            .SingleAsync();

        Assert.Equal(result.Id, fromDb.Id);
    }
}
