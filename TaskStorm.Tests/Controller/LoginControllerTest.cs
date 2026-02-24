using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Service;
using TaskStorm.Exception.LoginException;
using TaskStorm.Exception.UserException;
using Xunit;

namespace TaskStorm.Tests.Controller
{
    public class LoginControllerTests
    {
        private readonly Mock<ILoginService> _loginServiceMock;
        private readonly ILogger<LoginController> _logger;

        public LoginControllerTests()
        {
            _loginServiceMock = new Mock<ILoginService>();
            _logger = new LoggerFactory().CreateLogger<LoginController>();
        }

        private LoginController CreateController() => new LoginController(_logger, _loginServiceMock.Object);

        [Fact]
        public async Task Login_ShouldReturnOk_WhenCredentialsValid()
        {
            // arrange
            var controller = CreateController();
            var request = new LoginRequest("test@test.com", "password123");

            var accessToken = new AccessToken("access-token", DateTime.UtcNow.AddMinutes(5));
            var refreshTokenDto = new RefreshTokenDto("refresh-token", DateTime.UtcNow.AddMinutes(30));
            var tokenResponse = new TokenResponseDto(accessToken, refreshTokenDto);

            _loginServiceMock
                .Setup(s => s.LoginAsync(request))
                .ReturnsAsync(tokenResponse);

            // act
            var result = await controller.Login(request);

            // assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<TokenResponseDto>(okResult.Value);

            Assert.Equal(tokenResponse.AccessToken.Token, returnedDto.AccessToken.Token);
            Assert.Equal(tokenResponse.AccessToken.Expires, returnedDto.AccessToken.Expires);
            Assert.Equal(tokenResponse.RefreshToken.Token, returnedDto.RefreshToken.Token);
            Assert.Equal(tokenResponse.RefreshToken.Expires, returnedDto.RefreshToken.Expires);

            _loginServiceMock.Verify(s => s.LoginAsync(request), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenInvalidEmailOrPassword()
        {
            var controller = CreateController();
            var request = new LoginRequest("wrong@test.com", "password123");

            _loginServiceMock
                .Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new InvalidEmailOrPasswordException("Wrong email or password"));

            var result = await controller.Login(request);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("Wrong email or password", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenUserDisabled()
        {
            // arrange
            var controller = CreateController();
            var request = new LoginRequest("disabled@test.com", "password123");

            _loginServiceMock
                .Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new UserDisabledException("User account is disabled"));

            // act
            var result = await controller.Login(request);

            // assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("User account is disabled", unauthorizedResult.Value.ToString());
        }

        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            var controller = CreateController();

            var result = await controller.Login(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Request cannot be null", badRequest.Value);

            _loginServiceMock.Verify(s => s.LoginAsync(It.IsAny<LoginRequest>()), Times.Never);
        }
    }
}