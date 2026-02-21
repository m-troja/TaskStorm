using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;

namespace TaskStorm.Controller;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> l;
    private readonly IAuthService _authService;

    [HttpPost("regenerate-tokens")]
    public async Task<ActionResult<TokenResponseDto>> RegenerateTokensByRefreshToken([FromBody] RefreshTokenRequest req)
    {
        l.LogDebug($"Controller POST regenerate-tokens: {req.RefreshToken}");

        Boolean validated = false;
        try
        {
            validated = await _authService.ValidateRefreshTokenRequest(req.RefreshToken);
        }
        catch (InvalidRefreshTokenException ex)
        {
            l.LogError($"Validation failed: {ex}");
            return Unauthorized(new Response(ResponseType.ERROR, "Validation failed"));
        }
        catch (UserNotFoundException ex)
        {
            l.LogError($"User not found");
            return NotFound(new Response(ResponseType.ERROR, "User not found"));
        }
        catch (TokenRevokedException ex)
        {
            l.LogError($"Refresh token is revoked");
            return Unauthorized(new Response(ResponseType.ERROR, "Refresh token is revoked"));
        }
        catch (TokenExpiredException ex)
        {
            l.LogError($"Refresh token expired");
            return Unauthorized(new Response(ResponseType.ERROR, "Refresh token expired"));
        }
        if (validated)
        {
            var tokenDto = await _authService.RegenerateTokensByRefreshToken(req.RefreshToken);
            l.LogDebug($"Tokens regenerated successfully by refreshToken={req.RefreshToken}");  

            return Ok(tokenDto);
        }
        else
        {
            return Unauthorized(new Response(ResponseType.ERROR, "Validation failed"));
        }
    }

    public AuthController(ILogger<AuthController> l, IAuthService authService)
    {
        this.l = l;
        _authService = authService;
    }
}
