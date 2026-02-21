using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TaskStorm.Data;
using TaskStorm.Model.Entity;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class RoleServiceTests
{
    private PostgresqlDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PostgresqlDbContext(options);
        db.Users.RemoveRange(db.Users);
        db.Roles.RemoveRange(db.Roles);
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GetRoleByName_ShouldReturnRole_WhenExists()
    {
        // given
        await using var db = GetInMemoryDb();
        var role = new Role("ADMIN");
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var service = new RoleService(db);

        // when
        var result = await service.GetRoleByName("ADMIN");

        // then
        Assert.NotNull(result);
        Assert.Equal("ADMIN", result.Name);
    }

    [Fact]
    public async Task GetRoleByName_ShouldThrow_WhenNotExists()
    {
        await using var db = GetInMemoryDb();
        var service = new RoleService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.GetRoleByName("NON_EXISTENT_ROLE");
        });
    }
}
