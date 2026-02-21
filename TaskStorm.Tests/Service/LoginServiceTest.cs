using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using TaskStorm.Exception.LoginException;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service
{
    public class LoginServiceTest
    {
        private readonly Mock<IUserService> _mockUserService = new();
        private readonly Mock<ILogger<LoginService>> _mockLogger = new();
        private readonly Mock<IJwtGenerator> _mockJwt = new();
        private readonly Mock<IAuthService> _mockAuthService = new();
        private readonly Mock<IPasswordService> _passwordService = new();
        private readonly RefreshTokenCnv _refreshTokenCnv;
        public LoginServiceTest()
        {
            _refreshTokenCnv = new RefreshTokenCnv();

        }

        private LoginService CreateService()
        {
            return new LoginService(_mockUserService.Object, _mockLogger.Object, _passwordService.Object, _mockJwt.Object, _mockAuthService.Object, _refreshTokenCnv);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnUser_WhenCredentialsAreValid()
        {
            // Arrange
            var password = "securePassword";
            var salt = new byte[] { 0x01, 0x02, 0x03 };
            var hashedPassword = "hashedPassword";
            _passwordService.Setup(x => x.GenerateSalt()).Returns(salt);
            _passwordService.Setup(x => x.HashPassword(password, salt)).Returns(hashedPassword);

            var user = new User("TestUser", "test@example.com")
            {
                Id = 1,
                Salt = salt,
                Password = hashedPassword,
                Disabled = false
            }; 
            var at = new AccessToken("access-token", DateTime.UtcNow.AddMinutes(2));
            var rt = new RefreshToken("refresh-token", user.Id, DateTime.UtcNow.AddDays(7));
            var rtDto = _refreshTokenCnv.EntityToDto(rt);

            var tokenResponseDto = new TokenResponseDto(  at, rtDto);

            

            var request = new LoginRequest("test@example.com", password);

            _mockUserService.Setup(x => x.TryGetByEmailAsync("test@example.com")).ReturnsAsync(user);
            _mockAuthService.Setup(x => x.GetAccessTokenByUserId(user.Id)).Returns(at);    
            _mockAuthService.Setup(x => x.GenerateRefreshToken(user.Id)).ReturnsAsync(rt);
            var service = CreateService();

            // Act
            var result = await service.LoginAsync(request);

            // Assert
            Assert.NotNull(result);
            //Assert.Equal(user.Email, result.Email);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidEmailOrPasswordException_WhenPasswordIncorrect()
        {
            // Arrange
            var password = "securePassword";
            var salt = new byte[] { 0x01, 0x02, 0x03 };
            var hashedPassword = "hashedPassword";
            _passwordService.Setup(x => x.GenerateSalt()).Returns(salt);
            _passwordService.Setup(x => x.HashPassword(password, salt)).Returns(hashedPassword);

            var user = new User("TestUser", "test@example.com")
            {
                Id = 1,
                Salt = salt,
                Password = hashedPassword,
                Disabled = false
            };

            var request = new LoginRequest("test@example.com", "wrongPassword");

            _mockUserService.Setup(x => x.TryGetByEmailAsync("test@example.com")).ReturnsAsync(user);

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEmailOrPasswordException>(() => service.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUserDisabledException_WhenUserIsDisabled()
        {
            // Arrange
            var password = "securePassword";
            var salt = new byte[] { 0x01, 0x02, 0x03 };
            var hashedPassword = "hashedPassword";
            _passwordService.Setup(x => x.GenerateSalt()).Returns(salt);
            _passwordService.Setup(x => x.HashPassword(password, salt)).Returns(hashedPassword);

            var user = new User("TestUser", "test@example.com")
            {
                Id = 1,
                Salt = salt,
                Password = hashedPassword,
                Disabled = true
            };

            var request = new LoginRequest("test@example.com", password);

            _mockUserService.Setup(x => x.TryGetByEmailAsync("test@example.com")).ReturnsAsync(user);

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<UserDisabledException>(() => service.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidEmailOrPasswordException_WhenEmailInvalid()
        {
            // Arrange
            var request = new LoginRequest("invalid-email", "password");
            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEmailOrPasswordException>(() => service.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidEmailOrPasswordException_WhenEmailOrPasswordEmpty()
        {
            // Arrange
            var service = CreateService();

            var request1 = new LoginRequest("", "password");
            var request2 = new LoginRequest("test@example.com", "");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEmailOrPasswordException>(() => service.LoginAsync(request1));
            await Assert.ThrowsAsync<InvalidEmailOrPasswordException>(() => service.LoginAsync(request2));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidEmailOrPasswordException_WhenEmailOrPasswordTooLong()
        {
            // Arrange
            var longEmail = new string('a', 251) + "@example.com";
            var longPassword = new string('p', 251);

            var service = CreateService();

            var request1 = new LoginRequest(longEmail, "password");
            var request2 = new LoginRequest("test@example.com", longPassword);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEmailOrPasswordException>(() => service.LoginAsync(request1));
            await Assert.ThrowsAsync<InvalidEmailOrPasswordException>(() => service.LoginAsync(request2));
        }
    }
}
