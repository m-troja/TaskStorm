using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskStorm.Data;
using TaskStorm.Model.Entity;

namespace TaskStorm.Security.Impl;

public class JwtGenerator : IJwtGenerator
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _expiryMinutes;
    private readonly ILogger<JwtGenerator> _logger;
    private readonly PostgresqlDbContext _db;

    public JwtGenerator(IConfiguration config, ILogger<JwtGenerator> logger, PostgresqlDbContext db)
    {
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                     ?? throw new InvalidOperationException("JWT_SECRET not configured.");
        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                     ?? throw new InvalidOperationException("JWT_ISSUER not configured.");
        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                       ?? throw new InvalidOperationException("JWT_AUDIENCE not configured.");
        _expiryMinutes = int.TryParse(Environment.GetEnvironmentVariable("ACCESS_TOKEN_EXPIRY_MINUTES"), out var m) ? m : 60;
        _logger = logger;
        _db = db;
    }

    public AccessToken GenerateAccessToken(int userId)
    {
        _logger.LogDebug($"Generating JWT for userId {userId}");
        var roles = _db.Users
                       .Where(u => u.Id == userId)
                       .SelectMany(u => u.Roles.Select(r => r.Name))
                       .ToList();
        _logger.LogDebug($"Generating JWT : found roles: {string.Join(", ", roles)}");

        var claims = new List<Claim>();
        var claim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
        _logger.LogDebug($"Added claim for userId: {userId}");
        claims.Add(claim);

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        _logger.LogDebug($"Added role claims: {string.Join(", ", roles)}");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiry = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiry,
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        _logger.LogDebug($"JWT generated for userId {userId}, roles: {string.Join(", ", roles)}");

        return new AccessToken(tokenHandler.WriteToken(token), expiry);
    }

    public RefreshToken GenerateRefreshToken(int userId)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);

        var refreshToken = new RefreshToken(
            Convert.ToBase64String(bytes),
            userId,
            DateTime.UtcNow.AddDays(7)
        );

        _logger.LogDebug($"Refresh token generated for userId {userId}, expires {refreshToken.Expires}");

        return refreshToken;
    }
}