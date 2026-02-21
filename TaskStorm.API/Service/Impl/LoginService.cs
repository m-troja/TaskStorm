using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using TaskStorm.Exception.LoginException;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Security;

namespace TaskStorm.Service.Impl;

public class LoginService : ILoginService
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly ILogger<LoginService> logger;
    private readonly IPasswordService _passwordService;
    private readonly RefreshTokenCnv refreshTokenCnv;

    public LoginService(IUserService userService, ILogger<LoginService> logger, IPasswordService passwordService, IJwtGenerator jwtGenerator, IAuthService _authService, RefreshTokenCnv refreshTokenCnv)
    {
        _userService = userService;
        this.logger = logger;
        _passwordService = passwordService;
        this._authService = _authService;
        this.refreshTokenCnv = refreshTokenCnv;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequest request)
    {
        logger.LogDebug($"Attempting login for {request.email.ToLower()}");

        ValidateInput(request);

        var user = await _userService.TryGetByEmailAsync(request.email.ToLower());
        if (user == null)
        {
            logger.LogDebug("Login failed: user not found");
            throw new InvalidEmailOrPasswordException("Wrong email or password");
        }

        if (user.Disabled)
        {
            logger.LogDebug($"Login failed: user {user.Email} is disabled");
            throw new UserDisabledException("User account is disabled");
        }

        if (user.Salt == null)
            throw new ArgumentNullException(nameof(user.Salt));

        var hashedPassword = _passwordService.HashPassword(request.password, user.Salt);

        if (hashedPassword != user.Password)
        {
            logger.LogDebug("Login failed: wrong password");
            throw new InvalidEmailOrPasswordException("Wrong email or password");
        }

        var tokenDto = await GenerateTokensForUser(user);

        logger.LogDebug($"Generated tokens for user {user}: {tokenDto}");
        logger.LogDebug($"Login successful for {user.Email}");
        return tokenDto;
    }

    private async Task<TokenResponseDto> GenerateTokensForUser(User user)
    {
        var accessToken = _authService.GetAccessTokenByUserId(user.Id);
        var refreshToken = await _authService.GenerateRefreshToken(user.Id);
        var saved = await _userService.SaveRefreshTokenAsync(refreshToken);
        return new TokenResponseDto(accessToken, refreshTokenCnv.EntityToDto(refreshToken));
    }

    private void ValidateInput(LoginRequest request)
    {
        if (!new EmailAddressAttribute().IsValid(request.email))
        {
            logger.LogDebug("Login failed: invalid email format");
            throw new InvalidEmailOrPasswordException("Invalid email format");
        }

        if (string.IsNullOrWhiteSpace(request.password) || string.IsNullOrWhiteSpace(request.email))
        {
            logger.LogDebug("Login failed: empty email or password");
            throw new InvalidEmailOrPasswordException("Email or password cannot be empty");
        }

        if (request.password.Length > 250 || request.email.Length > 250)
        {
            logger.LogDebug("Login failed: email or password too long");
            throw new InvalidEmailOrPasswordException("Email or password too long - max 250 chars");
        }
    }
}
