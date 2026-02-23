using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStorm.Data;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class UserServiceTests
{
    private readonly Mock<IChatGptService> _gptMock = new Mock<IChatGptService>();
    private readonly Mock<IPasswordService> _passwordMock = new Mock<IPasswordService>();


    private readonly CommentCnv _commentCnv;
    private readonly TeamCnv _teamCnv;
    private readonly IssueCnv _issueCnv;
    private readonly UserCnv _userCnv;


    private PostgresqlDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PostgresqlDbContext(options);

        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        return new PostgresqlDbContext(options);
    }

    public UserServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(b => { });

        _commentCnv = new CommentCnv(loggerFactory.CreateLogger<CommentCnv>());
        _teamCnv = new TeamCnv(loggerFactory.CreateLogger<TeamCnv>());
        _issueCnv = new IssueCnv(
            _commentCnv,
            loggerFactory.CreateLogger<IssueCnv>(),
            _teamCnv
        );
        _userCnv = new UserCnv();
    }

    private UserService CreateService(PostgresqlDbContext db)
    {
        return new UserService(
            db,
             LoggerFactory.Create(b => { }).CreateLogger<UserService>(),
            _userCnv,
            _gptMock.Object,
            _passwordMock.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        var db = GetDb();

        var user = new User("test", "U123") { Email = "a@a.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service =  CreateService(db);

        var result = await service.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenNotFound()
    {
        var db = GetDb();

        var service =  CreateService(db);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetByIdAsync(999));
    }
    
    [Fact]
    public async Task CreateUserAsync_ShouldSaveUser()
    {
        var db = GetDb();

        var service =  CreateService(db);
        var user = new User("test", "U123") { Email = "a@a.com" };

        var result = await service.CreateUserAsync(user);

        Assert.Equal(1, db.Users.Count());
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
    {
        var db = GetDb();

        var user = new User("test", "U123") { Email = "a@a.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetByEmailAsync("a@a.com");

        Assert.NotNull(result);
        Assert.Equal("a@a.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUserNotFoundException_WhenNotFound()
    {
        var db = GetDb();
        var chatGpt = new Mock<IChatGptService>();
        var cnv = new UserCnv();
        var service = CreateService(db);

        await Assert.ThrowsAsync<UserNotFoundException>( () =>  service.GetByEmailAsync("missing@mail.com"));
    }

    [Fact]
    public async Task GetIdBySlackUserId_ShouldReturnLocalUser_WhenExists()
    {
        var db = GetDb();

        var user = new User("test", "U123");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        int id = await service.GetIdBySlackUserId("U123");

        Assert.Equal(user.Id, id);
    }


    [Fact]
    public async Task GetIdBySlackUserId_ShouldCallChatGpt_WhenNotFoundLocally()
    {
        var db = GetDb();

        // chatgpt zwraca usera
        _gptMock.Setup(x => x.GetAllChatGptUsersAsync())
               .ReturnsAsync(new List<User>
               {
                   new User("remoteUser", "U999") { Id = 77 }
               });

        // bot user
        var bot = new User("bot", "USLACKBOT") { Id = 1234 };
        db.Users.Add(bot);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        int id = await service.GetIdBySlackUserId("U999");

        Assert.Equal(77, id);
    }


    [Fact]
    public async Task GetIdBySlackUserId_ShouldReturnBot_WhenUserNotFoundAnywhere()
    {
        var db = GetDb();

        _gptMock.Setup(x => x.GetAllChatGptUsersAsync())
               .ReturnsAsync(new List<User>());

        var bot = new User("bot", "USLACKBOT") { Id = 555 };
        db.Users.Add(bot);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        int id = await service.GetIdBySlackUserId("UnknownSlackUser");

        Assert.Equal(555, id);
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ShouldReturnFalseWhenWhenRefreshTokenExists()
    {
        var db = GetDb();
        var refreshToken = new RefreshToken("token", 1, DateTime.UtcNow.AddMinutes(2));
        var user = new User("test", "U123") { Id = 1 };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        Assert.Equal(1, user.RefreshTokens.Count);
        Assert.Equal(refreshToken, user.RefreshTokens.First());

        var service = CreateService(db);
        var result = await service.SaveRefreshTokenAsync(refreshToken);
        Assert.False(result);
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ShouldReturnFalseWhenWhenRefreshTokenIsExpired()
    {
        var db = GetDb();
        var refreshToken = new RefreshToken("token", 1, DateTime.UtcNow.AddMinutes(-2));
        var user = new User("test", "U123") { Id = 1 };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        Assert.Equal(1, user.RefreshTokens.Count);
        Assert.Equal(refreshToken, user.RefreshTokens.First());

        var service = CreateService(db);
        var result = await service.SaveRefreshTokenAsync(refreshToken);
        Assert.False(result);
    }
}
