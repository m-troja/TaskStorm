using Microsoft.EntityFrameworkCore;
using TaskStorm.Data;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Response;
using TaskStorm.Security;
namespace TaskStorm.Service.Impl;

public class AuthService : IAuthService
{
    private readonly PostgresqlDbContext _db;
    private readonly ILogger<AuthService> l;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly RefreshTokenCnv refreshTokenCnv;

    public AccessToken GetAccessTokenByUserId(int userId)
    {
        var AccessToken =  _jwtGenerator.GenerateAccessToken(userId);
        l.LogDebug($"Generated access token for user {userId}");
        return AccessToken;     
    }

    public async Task<AccessToken> GetAccessTokenByRefreshToken(string refreshToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync( u => u.RefreshTokens.Any(rt => rt.Token == refreshToken)) 
            ?? throw new UserNotFoundException("User not found for the provided refresh token");
        var AccessToken = _jwtGenerator.GenerateAccessToken(user.Id);
        l.LogDebug($"Generated access token for user {user.Id}: {AccessToken}");
        return AccessToken;
    }


    public async Task<RefreshToken> GenerateRefreshToken(int UserId)
    {
        var userByUserId = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId) ?? throw new UserNotFoundException("User not found");

        var NewRefreshToken = _jwtGenerator.GenerateRefreshToken(UserId);
        l.LogDebug($"Generated refresh token for userId {userByUserId.Id}: {NewRefreshToken.Token}, expires: {NewRefreshToken.Expires}");
        userByUserId.RefreshTokens.Add(NewRefreshToken);
        _db.Users.Update(userByUserId);
        await _db.SaveChangesAsync();

        return NewRefreshToken;
    }

    private async Task ValidateRefreshTokenRequest(string refreshToken)
    {
        var refreshTokenFromDb = _db.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
        var userByRefreshToken = await _db.Users.FirstOrDefaultAsync( u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));
        if (userByRefreshToken == null || refreshTokenFromDb == null || refreshTokenFromDb.UserId != userByRefreshToken.Id )
            {
            l.LogDebug("Refresh token or user not found");
            throw new InvalidRefreshTokenException("Refresh token or user not found");
        }
        if (refreshTokenFromDb.IsRevoked)
        {
            l.LogError("Refresh token is revoked");
            throw new TokenRevokedException("Refresh token is revoked");
        }
        if (refreshTokenFromDb.Expires < DateTime.UtcNow)
        {
            l.LogError("Refresh token expired");
            throw new TokenExpiredException("Refresh token expired");
        }
        l.LogDebug("Token validated succesffully");
    }
    public async Task<TokenResponseDto> RegenerateTokensByRefreshToken(string oldRefreshToken)
    {
        await ValidateRefreshTokenRequest(oldRefreshToken);

        var oldToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == oldRefreshToken) ?? throw new InvalidRefreshTokenException("Refresh token not found");
        if (oldToken != null && !oldToken.IsRevoked)
        {
            oldToken.IsRevoked = true;
        }
        var userByRefreshToken = await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == oldRefreshToken)) ?? throw new UserNotFoundException("User was not found");
        var NewRefreshToken = _jwtGenerator.GenerateRefreshToken(userByRefreshToken.Id);

        await _db.RefreshTokens.AddAsync(NewRefreshToken);
        await _db.SaveChangesAsync();

        var refreshTokenDto = refreshTokenCnv.EntityToDto(NewRefreshToken);
        return new TokenResponseDto( 
            _jwtGenerator.GenerateAccessToken(userByRefreshToken.Id),
            refreshTokenDto
        );

    }
    public AuthService(PostgresqlDbContext db, ILogger<AuthService> l, IJwtGenerator jwtGenerator, 
        RefreshTokenCnv refreshTokenCnv)
    {
        this.refreshTokenCnv = refreshTokenCnv;
        _db = db;
        this.l = l;
        _jwtGenerator = jwtGenerator;
    }
}
