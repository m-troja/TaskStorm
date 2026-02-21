using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStorm.Controller;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Controller;

public class LoginControllerTest
{
    private LoginController CreateController( Mock<ILoginService> loginService )
    {   
        var refreshTokenCnv = new RefreshTokenCnv();
        var logger = new LoggerFactory().CreateLogger<LoginService>();
        return new LoginController(logger, loginService.Object);
    }

    [Fact]
    public async Task Login_ShouldReturnTokenResponse_WhenCredentialsAreValid()
    {
        // arrange
        var loginService = new Mock<ILoginService>();

        var controller = CreateController(loginService);

        var request = new LoginRequest("user@test.com", "password");
        var user = new User { Id = 5, Email = "user@test.com" };
        var accessToken = new AccessToken("new-access-token", DateTime.UtcNow.AddMinutes(2));
        TokenResponseDto tokenResponseDto = new TokenResponseDto( accessToken, 
            new Model.DTO.RefreshTokenDto("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );
        loginService.Setup(s => s.LoginAsync(request)).ReturnsAsync(tokenResponseDto);

        // act
        var result = await controller.Login(request);

        // assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TokenResponseDto>(ok.Value);

        Assert.Equal("new-access-token", dto.AccessToken.Token);
        Assert.Equal("new-refresh-token", dto.RefreshToken.Token);

        loginService.Verify(s => s.LoginAsync(request), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserIsNull()
    {
        // arrange
        var loginService = new Mock<ILoginService>();
        var controller = CreateController(loginService);

        var request = new LoginRequest("user@test.com", "pw123");

        loginService
            .Setup(s => s.LoginAsync(request))
            .ReturnsAsync((TokenResponseDto?)null);

        // act
        var result = await controller.Login(request);

        // assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<Response>(unauthorized.Value);

        Assert.Equal(ResponseType.ERROR, response.responseType);
        Assert.Contains("user@test.com", response.message);
    }


    [Fact]
    public async Task Login_ShouldReturnNullAccessToken_WhenAuthServiceFails()
    {
        // arrange
        var loginService = new Mock<ILoginService>();
        var controller = CreateController(loginService);

        var request = new LoginRequest("user@test.com", "pw");
        var user = new User { Id = 77, Email = "user@test.com" };

        var accessToken = new AccessToken("new-access-token", DateTime.UtcNow.AddMinutes(2));
        TokenResponseDto tokenResponseDto = new TokenResponseDto(accessToken,
            new Model.DTO.RefreshTokenDto("token", DateTime.UtcNow.AddDays(7))
        );
        loginService.Setup(s => s.LoginAsync(request)).ReturnsAsync(tokenResponseDto);

        // act
        var result = await controller.Login(request);

        // assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TokenResponseDto>(ok.Value);

        Assert.Equal("token", dto.RefreshToken.Token);

        loginService.Verify(s => s.LoginAsync(request), Times.Once);
    }
}
