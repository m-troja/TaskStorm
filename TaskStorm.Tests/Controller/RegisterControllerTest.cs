using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
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

public class RegisterControllerTest
{
    private RegisterController CreateController(
        Mock<IRegisterService> rsMock)
    {
        var logger = new LoggerFactory().CreateLogger<RegisterController>();
        var userCnv = new UserCnv();
        return new RegisterController(rsMock.Object, logger, userCnv);
    }

    // 1) SUCCESSFUL REGISTRATION
    [Fact]
    public async Task RegisterUser_ShouldReturnOk_WhenRegistrationSucceeds()
    {
        // arrange
        var rsMock = new Mock<IRegisterService>();
        var user = new User("Jan", "Kowalski", "test@example.com", "hashed", new byte[] { 1 }, new Role(Role.ROLE_USER));
        var req = new RegistrationRequest("Jan", "Kowalski", "test@example.com", "hashed", "U123456");
        rsMock.Setup(s => s.Register(req)).ReturnsAsync(user);

        var controller = CreateController(rsMock);


        // act
        var result = await controller.RegisterUser(req);

        // assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(ok.Value);

        Assert.Equal("Jan", dto.FirstName);
        Assert.Equal("Kowalski", dto.LastName);
        Assert.Equal("test@example.com", dto.Email);

        rsMock.Verify(s => s.Register(req), Times.Once);
    }

    // 2) USER ALREADY EXISTS -> Conflict
    [Fact]
    public async Task RegisterUser_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        // arrange
        var rsMock = new Mock<IRegisterService>();
        rsMock.Setup(s => s.Register(It.IsAny<RegistrationRequest>()))
              .ThrowsAsync(new UserAlreadyExistsException("Email already registered"));

        var controller = CreateController(rsMock);
        var req = new RegistrationRequest("Jan", "Kowalski", "test@example.com", "hashed", "U123456");

        // act
        var result = await controller.RegisterUser(req);

        // assert
        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("Email already registered", conflict.Value.ToString());
    }

    // 3) INVALID REGISTRATION -> BadRequest
    [Fact]
    public async Task RegisterUser_ShouldReturnBadRequest_WhenInvalidData()
    {
        // arrange
        var rsMock = new Mock<IRegisterService>();
        var req = new RegistrationRequest("", "", "", "", "");
        rsMock.Setup(s => s.Register(req))
              .ThrowsAsync(new RegisterEmailException("Missing required fields"));

        var controller = CreateController(rsMock);

        // act
        var result = await controller.RegisterUser(req);

        // assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Missing required fields", badRequest.Value.ToString());
    }

    // 4) NULL REQUEST -> BadRequest
    [Fact]
    public async Task RegisterUser_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // arrange
        var rsMock = new Mock<IRegisterService>();
        var controller = CreateController(rsMock);

        // act
        var result = await controller.RegisterUser(null!);

        // assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Request cannot be null", badRequest.Value.ToString());

        rsMock.Verify(s => s.Register(It.IsAny<RegistrationRequest>()), Times.Never);
    }
    private UserDto BuildUserDto(string slackUserId)
    {
        return new UserDto(
            1,
            "John",
            "Doe",
            "email@test.com",
            new List<string>() { Role.ROLE_USER },
            new List<string>() { },
            false,
            slackUserId
            );
    }
} 