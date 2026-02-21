using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskStorm.Log;
using TaskStorm.Model.Entity;

namespace TaskStorm.Security.Impl;

public class JwtGenerator : IJwtGenerator
{
    private readonly string JWT_SECRET;
    private readonly string JWT_ISSUER;
    private readonly string JWT_AUDIENCE;
    private readonly ILogger<IJwtGenerator> l;
    private readonly string ExpiryMinutes;

    public JwtGenerator(IConfiguration config, ILogger<IJwtGenerator> logger)
    {
        JWT_SECRET = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET not configured."); 
        JWT_ISSUER = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new InvalidOperationException("JWT_ISSUER not configured.");
        JWT_AUDIENCE = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new InvalidOperationException("JWT_AUDIENCE not configured.");
        ExpiryMinutes = Environment.GetEnvironmentVariable("ACCESS_TOKEN_EXPIRY_MINUTES") ?? "2";
        l = logger;
    }

    public AccessToken GenerateAccessToken(int userId)
    {
        l.LogDebug($"Generating access token for userId {userId}");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JWT_SECRET);
        var expiry = DateTime.UtcNow.AddMinutes(int.Parse(ExpiryMinutes));

        var claims = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        });

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = expiry,
            Issuer = JWT_ISSUER,
            Audience = JWT_AUDIENCE,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = new AccessToken(tokenHandler.WriteToken(token), expiry);
        l.LogDebug($"Access token generated: {accessToken}");
        return accessToken;
    }

    public RefreshToken GenerateRefreshToken(int userId)
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            var RefreshToken = new RefreshToken(
                Convert.ToBase64String(randomNumber), userId, DateTime.UtcNow.AddDays(7)
                );
            l.LogDebug($"Refresh token: {RefreshToken.Token}, expires: {RefreshToken.Expires}");
            return RefreshToken; 
            
        }
    }
}
