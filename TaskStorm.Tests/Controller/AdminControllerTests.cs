using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
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
    private readonly Mock<IActivityService> _activityService = new();

    private readonly UserCnv _userCnv = new(); 

    private readonly AdminController _controller;


    public AdminControllerTests()
    {
        _controller = new AdminController(
            _loggerMock.Object,
            _authServiceMock.Object,
            _userServiceMock.Object,
            _userCnv,
            _activityService.Object
        );
    }


    [Fact]
    public async Task ResetPassword_WhenUserNotFound_ShouldThrow()
    {
        SetUser(1, Role.ROLE_ADMIN);

        var request = new ResetPasswordRequest(99, "Test123!");

        _userServiceMock
            .Setup(s => s.ResetPassword(request))
            .ThrowsAsync(new UserNotFoundException("Not found"));

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _controller.ResetPassword(request)
        );
    }

    private void SetUser(int id, string role)
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ClaimTypes.Role, role)
    };

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}