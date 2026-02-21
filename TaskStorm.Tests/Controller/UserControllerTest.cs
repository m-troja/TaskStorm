using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;

public class UserControllerTest
{
    private UserController CreateController(Mock<IUserService> userServiceMock)
    {
        var userCnv = new UserCnv();
        var logger = new LoggerFactory().CreateLogger<UserController>();
        return new UserController(userServiceMock.Object, userCnv, logger);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WhenUserExists()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        var role = new Role { Name = "USER" };
        var user = new User("John", "Doe", "john@test.com", "hashed", new byte[] { 1 }, role) { Id = 1 };
        userServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(user);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetUserById(1);

        // THEN
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("john@test.com", dto.Email);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((User)null);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetUserById(1);

        // THEN
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("was not found", notFound.Value.ToString());
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnOk_WhenUserExists()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        var role = new Role { Name = "USER" };
        var user = new User("Jane", "Doe", "jane@test.com", "hashed", new byte[] { 1 }, role);
        userServiceMock.Setup(s => s.GetByEmailAsync("jane@test.com")).ReturnsAsync(user);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetUserByEmail("jane@test.com");

        // THEN
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("jane@test.com", dto.Email);
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(s => s.GetByEmailAsync("unknown@test.com")).ReturnsAsync((User)null);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetUserByEmail("unknown@test.com");

        // THEN
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("was not found", notFound.Value.ToString());
    }

    [Fact]
    public async Task DeleteUserById_ShouldReturnOk_WhenUserExists()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        var user = new User("John", "Doe", "john@test.com", "hashed", new byte[] { 1 }, null) { Id = 1 };
        userServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(user);
        userServiceMock.Setup(s => s.DeleteUserById(1)).Returns(Task.CompletedTask);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.DeleteUserById(1);

        // THEN
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Contains("Deleted user 1", okResult.Value.ToString());
        userServiceMock.Verify(s => s.DeleteUserById(1), Times.Once);
    }

    [Fact]
    public async Task DeleteUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((User)null);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.DeleteUserById(1);

        // THEN
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("was not found", notFound.Value.ToString());
    }

    [Fact]
    public async Task DeleteAllUsers_ShouldReturnOk()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(s => s.DeleteAllUsers()).Returns(Task.CompletedTask);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.DeleteAllUsers();

        // THEN
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Contains("All users deleted successfully", okResult.Value.ToString());
        userServiceMock.Verify(s => s.DeleteAllUsers(), Times.Once);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WhenUserHasSlackId()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        var user = new User("SlackUser", "SLACK123") { Id = 42 };
        userServiceMock.Setup(s => s.GetByIdAsync(42)).ReturnsAsync(user);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetUserById(42);

        // THEN
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("SlackUser", dto.FirstName);
        Assert.Equal("SLACK123", dto.userSlackId);
    }

    [Fact]
    public async Task GetUserByEmail_ShouldReturnNotFound_WhenUserHasSlackIdAndNoEmail()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        var user = new User("SlackUser", "SLACK123"); // Email null
        userServiceMock.Setup(s => s.GetByEmailAsync("noemail@test.com")).ReturnsAsync((User)null);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetUserByEmail("noemail@test.com");

        // THEN
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("was not found", notFound.Value.ToString());
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnListOfUserDtos_WithSlackIdAndRoles()
    {
        // GIVEN
        var userServiceMock = new Mock<IUserService>();
        var roleAdmin = new Role { Name = "ADMIN" };
        var roleUser = new Role { Name = "USER" };

        var users = new List<User>
        {
            new User("John", "Doe", "john@test.com", "hashed", new byte[] {1}, roleAdmin) { Id = 1 },
            new User("SlackUser", "SLACK123") { Id = 2, Roles = new List<Role> { roleUser } }
        };

        userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

        var controller = CreateController(userServiceMock);

        // WHEN
        var result = await controller.GetAllUsers();

        // THEN
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtoList = Assert.IsType<List<UserDto>>(okResult.Value);

        Assert.Equal(2, dtoList.Count);

        var firstUser = dtoList[0];
        Assert.Equal("John", firstUser.FirstName);
        Assert.Equal("Doe", firstUser.LastName);
        Assert.Equal("john@test.com", firstUser.Email);
        Assert.Contains("ADMIN", firstUser.Roles);

        var secondUser = dtoList[1];
        Assert.Equal("SlackUser", secondUser.FirstName);
        Assert.Equal("SLACK123", secondUser.userSlackId);
        Assert.Contains("USER", secondUser.Roles);

        userServiceMock.Verify(s => s.GetAllUsersAsync(), Times.Once);
    }
}
