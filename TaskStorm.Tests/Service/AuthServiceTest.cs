using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStorm.Data;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;

public class AuthServiceTest
{
    // --- Header mocks ---
    private readonly Mock<IJwtGenerator> _jwtMock;
    private readonly ILogger<AuthService> _logger;
    private readonly RefreshTokenCnv _refreshTokenCnv;

    public AuthServiceTest()
    {
        _jwtMock = new Mock<IJwtGenerator>();
        _logger = new LoggerFactory().CreateLogger<AuthService>();
        _refreshTokenCnv = new RefreshTokenCnv();
    }

    // --- Helper: in-memory DB ---
    private static PostgresqlDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PostgresqlDbContext(options);
    }

    // --- Helper: create service ---
    private AuthService CreateService(PostgresqlDbContext db)
    {
        return new AuthService(db, _logger, _jwtMock.Object, _refreshTokenCnv);
    }

    // --- Helper: seed full user ---
    private static User SeedUser(PostgresqlDbContext db, int id = 1)
    {
        var user = new User
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            Email = "test@test.com",
            Password = "hashed",
            Salt = new byte[] { 1, 2, 3 },
            Roles = new List<Role> { new Role(Role.ROLE_USER) },
            RefreshTokens = new List<RefreshToken>()
        };

        db.Users.Add(user);
        db.SaveChanges();

        return user;
    }

    // --- Tests ---

    [Fact]
    public void GetAccessTokenByUserId_ShouldGenerateToken()
    {
        var db = GetDb();
        var service = CreateService(db);

        _jwtMock.Setup(x => x.GenerateAccessToken(1))
                .Returns(new AccessToken("access", DateTime.UtcNow.AddMinutes(5)));

        var token = service.GetAccessTokenByUserId(1);

        Assert.Equal("access", token.Token);
        _jwtMock.Verify(x => x.GenerateAccessToken(1), Times.Once);
    }

    [Fact]
    public async Task GetAccessTokenByRefreshToken_ShouldReturnAccessToken()
    {
        var db = GetDb();
        var user = SeedUser(db);

        var refresh = new RefreshToken
        {
            Token = "refresh-123",
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddMinutes(10)
        };

        user.RefreshTokens.Add(refresh);
        db.SaveChanges();

        _jwtMock.Setup(x => x.GenerateAccessToken(user.Id))
                .Returns(new AccessToken("access", DateTime.UtcNow.AddMinutes(5)));

        var service = CreateService(db);

        var token = await service.GetAccessTokenByRefreshToken("refresh-123");

        Assert.Equal("access", token.Token);
    }

    [Fact]
    public async Task GetAccessTokenByRefreshToken_ShouldThrow_WhenUserNotFound()
    {
        var service = CreateService(GetDb());

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.GetAccessTokenByRefreshToken("invalid"));
    }

    [Fact]
    public async Task GenerateRefreshToken_ShouldPersistToken()
    {
        var db = GetDb();
        var user = SeedUser(db);

        var refresh = new RefreshToken
        {
            Token = "new-refresh",
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        _jwtMock.Setup(x => x.GenerateRefreshToken(user.Id))
                .Returns(refresh);

        var service = CreateService(db);

        var result = await service.GenerateRefreshToken(user.Id);

        Assert.Equal("new-refresh", result.Token);
        Assert.Single(db.RefreshTokens);
    }

    [Fact]
    public async Task GenerateRefreshToken_ShouldThrow_WhenUserNotFound()
    {
        var service = CreateService(GetDb());

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.GenerateRefreshToken(999));
    }

    [Fact]
    public async Task RegenerateTokens_ShouldRevokeOldAndCreateNew()
    {
        var db = GetDb();
        var user = SeedUser(db);

        var oldToken = new RefreshToken
        {
            Token = "old",
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddMinutes(10),
            IsRevoked = false
        };

        db.RefreshTokens.Add(oldToken);
        user.RefreshTokens.Add(oldToken);
        db.SaveChanges();

        _jwtMock.Setup(x => x.GenerateRefreshToken(user.Id))
                .Returns(new RefreshToken { Token = "new", UserId = user.Id, Expires = DateTime.UtcNow.AddMinutes(30) });

        _jwtMock.Setup(x => x.GenerateAccessToken(user.Id))
                .Returns(new AccessToken("access", DateTime.UtcNow.AddMinutes(5)));

        var service = CreateService(db);

        var response = await service.RegenerateTokensByRefreshToken("old");

        Assert.True(oldToken.IsRevoked);
        Assert.Equal("new", response.RefreshToken.Token);
        Assert.Equal("access", response.AccessToken.Token);
    }

    [Fact]
    public async Task RegenerateTokens_ShouldThrow_WhenTokenRevoked()
    {
        var db = GetDb();
        var user = SeedUser(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "revoked",
            UserId = user.Id,
            IsRevoked = true,
            Expires = DateTime.UtcNow.AddMinutes(10)
        });
        db.SaveChanges();

        var service = CreateService(db);

        await Assert.ThrowsAsync<TokenRevokedException>(() =>
            service.RegenerateTokensByRefreshToken("revoked"));
    }

    [Fact]
    public async Task RegenerateTokens_ShouldThrow_WhenTokenExpired()
    {
        var db = GetDb();
        var user = SeedUser(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "expired",
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddMinutes(-1)
        });
        db.SaveChanges();

        var service = CreateService(db);

        await Assert.ThrowsAsync<TokenExpiredException>(() =>
            service.RegenerateTokensByRefreshToken("expired"));
    }
}