using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStorm.Controller;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using Xunit;
namespace TaskStorm.Tests.Controller;

public class AdminControllerTests
{
    private readonly Mock<ILogger<AuthController>> _loggerMock = new();
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();

    private readonly UserCnv _userCnv = new(); 

    private readonly AdminController _controller;


    public AdminControllerTests()
    {
        _controller = new AdminController(
            _loggerMock.Object,
            _authServiceMock.Object,
            _userServiceMock.Object,
            _userCnv
        );
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnOk_WithMappedUserDto()
    {
        // Arrange
        var request = new ResetPasswordRequest(
            1,
            "NewPassword123!"
        );

        var role = new Role(Role.ROLE_ADMIN);

        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            SlackUserId = "U123",
            Disabled = false,
            Roles = new List<Role> { role },
            Teams = new List<Team>() 
        };

        _userServiceMock
            .Setup(s => s.ResetPassword(request))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        var returnedDto = Assert.IsType<UserDto>(okResult.Value);

        Assert.Equal(user.Id, returnedDto.Id);
        Assert.Equal(user.FirstName, returnedDto.FirstName);
        Assert.Equal(user.LastName, returnedDto.LastName);
        Assert.Equal(user.Email, returnedDto.Email);
        Assert.Equal(user.SlackUserId, returnedDto.userSlackId);
        Assert.Equal(user.Disabled, returnedDto.Disabled);
        Assert.Contains(Role.ROLE_ADMIN, returnedDto.Roles);
        Assert.Empty(returnedDto.Teams);

        _userServiceMock.Verify(s => s.ResetPassword(request), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WhenUserNotFound_ShouldThrow()
    {
        var request = new ResetPasswordRequest(99, "Test123!");

        _userServiceMock
            .Setup(s => s.ResetPassword(request))
            .ThrowsAsync(new UserNotFoundException("Not found"));

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _controller.ResetPassword(request)
        );
    }
}