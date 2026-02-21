using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using TaskStorm.Data;
using TaskStorm.Exception.Registration;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class RegisterServiceTests
{
    private PostgresqlDbContext GetDb()
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

    private PasswordService GetPasswordService()
    {
        return new PasswordService(new LoggerFactory().CreateLogger<PasswordService>());
    }

    private RegisterService CreateService(
        PostgresqlDbContext db,
        Mock<IUserService> mockUserService,
        Mock<IRoleService> mockRoleService)
    {
        var mockLogger = new Mock<ILogger<RegisterService>>();
        var mockChatGpt = new Mock<IChatGptService>();
        return new RegisterService(db, mockUserService.Object, mockRoleService.Object, GetPasswordService(), mockLogger.Object, mockChatGpt.Object);
    }

    [Fact]
    public async Task Register_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        var db = GetDb();
        var mockUserService = new Mock<IUserService>();
        var mockRoleService = new Mock<IRoleService>();
        var role = new Role(Role.ROLE_USER);
        mockRoleService.Setup(x => x.GetRoleByName(Role.ROLE_USER)).ReturnsAsync(role);
        mockUserService.Setup(x => x.TryGetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

        var service = CreateService(db, mockUserService, mockRoleService);

        var request = new RegistrationRequest("Jan", "Kowalski", "test@example.com", "hashed", "U123456");


        // Act
        await service.Register(request);

        // Assert
        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
        Assert.NotNull(userInDb);
        Assert.Equal(request.FirstName, userInDb.FirstName);
        Assert.Equal(request.LastName, userInDb.LastName);
    }

    [Fact]
    public async Task Register_ShouldThrowRegisterException_WhenFieldsMissing()
    {
        var db = GetDb();
        var mockUserService = new Mock<IUserService>();
        var mockRoleService = new Mock<IRoleService>();
        var service = CreateService(db, mockUserService, mockRoleService);

        var request = new RegistrationRequest(
            null,        
            "Doe",
            "test@example.com",
            "Password123!",
            "U123456"
        );

        await Assert.ThrowsAsync<RegisterEmailException>(() => service.Register(request));
    }

    [Fact]
    public async Task Register_ShouldThrowRegisterEmailException_WhenEmailInvalid()
    {
        // Arrange
        var db = GetDb();
        var mockUserService = new Mock<IUserService>();
        var mockRoleService = new Mock<IRoleService>();
        var service = CreateService(db, mockUserService, mockRoleService);

        var request = new RegistrationRequest(
            "John",
            "Doe",
            "invalid-email", 
            "Password123!",
            "U123456"
        );

        // Act & Assert
        await Assert.ThrowsAsync<RegisterEmailException>(() => service.Register(request));
    }

    [Fact]
    public async Task Register_ShouldThrowRegisterEmailException_WhenEmailExists()
    {
        var db = GetDb();
        var mockUserService = new Mock<IUserService>();
        var mockRoleService = new Mock<IRoleService>();

        var existingUser = new User("John", "Doe", "test@example.com", "hashed", new byte[16], new Role(Role.ROLE_USER));
        mockUserService.Setup(x => x.TryGetByEmailAsync("test@example.com")).ReturnsAsync(existingUser);

        var service = CreateService(db, mockUserService, mockRoleService);

        var request = new RegistrationRequest(
            "John",
            "Doe",
            "invalid-email",
            "Password123!",
            "U123456"
        );


        await Assert.ThrowsAsync<RegisterEmailException>(() => service.Register(request));
    }
}