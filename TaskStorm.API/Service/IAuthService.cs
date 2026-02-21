using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;

namespace TaskStorm.Service;

public interface IAuthService
{
    AccessToken GetAccessTokenByUserId(int userId);
    Task<RefreshToken> GenerateRefreshToken(int UserId);
    Task<Boolean> ValidateRefreshTokenRequest(string refreshToken);
    Task<AccessToken> GetAccessTokenByRefreshToken(string refreshToken);
    Task<TokenResponseDto> RegenerateTokensByRefreshToken(string refreshToken);
}
