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
using TaskStorm.Model.Response;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Controller;

public class AuthControllerTests
{
    private readonly Mock<ILogger<LoginService>> _mockLogger = new();
    private readonly Mock<IAuthService> _mockAuthService = new();

    private AuthController CreateController() {
        return new AuthController (_mockLogger.Object, _mockAuthService.Object);
    }

    [Fact]
    public async Task RegenerateTokens_ShouldReturnOk_WhenRefreshTokenIsValid()
    {

        // given
        var req = new RefreshTokenRequest("valid-token");
        var tokenResponseDto = new TokenResponseDto(
           new AccessToken("new-access-token", DateTime.UtcNow.AddMinutes(2)),
            new RefreshTokenDto("token", DateTime.UtcNow.AddDays(7))
            );

        _mockAuthService.Setup(x => x.RegenerateTokensByRefreshToken(req.RefreshToken)).ReturnsAsync(tokenResponseDto);
        _mockAuthService.Setup(x => x.ValidateRefreshTokenRequest(req.RefreshToken)).ReturnsAsync(true);

        var controller = CreateController();

        // when
        var result = await controller.RegenerateTokensByRefreshToken(req);

        // then
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tokenResponse = Assert.IsType<TokenResponseDto>(okResult.Value);

        Assert.Equal(tokenResponseDto, tokenResponse);
    }

    [Fact]
    public async Task RegenerateTokens_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        // given
        var req = new RefreshTokenRequest("invalid-token");
        var tokenResponseDto = new TokenResponseDto(
            new AccessToken("access-token", DateTime.Parse("2046-02-01T00:00:00Z")),
            new RefreshTokenDto("refresh-token", DateTime.Parse("2024-02-01T00:00:00Z"))
            );

        _mockAuthService.Setup(x => x.RegenerateTokensByRefreshToken(req.RefreshToken)).ReturnsAsync(tokenResponseDto);

        var controller = CreateController();

        // when
        var result = await controller.RegenerateTokensByRefreshToken(req);

        // then
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<Response>(unauthorizedResult.Value);

        Assert.Equal(ResponseType.ERROR, response.responseType);
        Assert.Equal("Validation failed", response.message);
    }
}