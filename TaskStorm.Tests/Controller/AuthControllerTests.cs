using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Exception.Tokens;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(mockLogger.Object, _mockAuthService.Object);
    }

    [Fact]
    public async Task RegenerateTokens_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var invalidToken = "invalid-token";

        _mockAuthService
            .Setup(s => s.RegenerateTokensByRefreshToken(invalidToken))
            .ThrowsAsync(new InvalidRefreshTokenException("Invalid token"));

        var request = new RefreshTokenRequest(invalidToken);

        // Act
        var result = await _controller.RegenerateTokensByRefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("Invalid token", unauthorizedResult.Value);
    }

    [Fact]
    public async Task RegenerateTokens_ShouldReturnOk_WhenRefreshTokenIsValid()
    {
        // Arrange
        var validToken = "valid-token";
        var tokenResponse = new TokenResponseDto(null, null);

        _mockAuthService
            .Setup(s => s.RegenerateTokensByRefreshToken(validToken))
            .ReturnsAsync(tokenResponse);

        var request = new RefreshTokenRequest(validToken);

        // Act
        var result = await _controller.RegenerateTokensByRefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(tokenResponse, okResult.Value);
    }
}