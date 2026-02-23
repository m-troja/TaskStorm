using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Exception.Registration;
using TaskStorm.Exception.UserException;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;

public class RegisterControllerTests
{
    private readonly Mock<IRegisterService> _registerServiceMock;
    private readonly ILogger<RegisterController> _logger;
    private readonly UserCnv _userCnv;

    public RegisterControllerTests()
    {
        _registerServiceMock = new Mock<IRegisterService>();
        _logger = new LoggerFactory().CreateLogger<RegisterController>();
        _userCnv = new UserCnv();
    }

    private RegisterController CreateController() =>
        new RegisterController(_registerServiceMock.Object, _logger, _userCnv);

    [Fact]
    public async Task RegisterUser_ShouldReturnOk_WhenRegistrationSucceeds()
    {
        var controller = CreateController();
        var req = new RegistrationRequest("John", "Doe", "test@example.com", "hashed", "U123");

        var user = new User("John", "Doe", "test@example.com", "hashed", new byte[] { 1 }, new Role(Role.ROLE_USER));
        _registerServiceMock.Setup(s => s.Register(req)).ReturnsAsync(user);

        var result = await controller.RegisterUser(req);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(okResult.Value);

        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("test@example.com", dto.Email);

        _registerServiceMock.Verify(s => s.Register(req), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        var controller = CreateController();

        var result = await controller.RegisterUser(null!);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Request cannot be null", badResult.Value);

        _registerServiceMock.Verify(s => s.Register(It.IsAny<RegistrationRequest>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnBadRequest_WhenEmailInvalid()
    {
        var controller = CreateController();
        var req = new RegistrationRequest("John", "Doe", "invalid", "hashed", "U123");

        _registerServiceMock.Setup(s => s.Register(req))
            .ThrowsAsync(new RegisterEmailException("Invalid email"));

        var result = await controller.RegisterUser(req);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid email", badResult.Value.ToString());
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        var controller = CreateController();
        var req = new RegistrationRequest("John", "Doe", "test@example.com", "hashed", "U123");

        _registerServiceMock.Setup(s => s.Register(req))
            .ThrowsAsync(new UserAlreadyExistsException("User exists"));

        var result = await controller.RegisterUser(req);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("User exists", conflictResult.Value.ToString());
    }
}