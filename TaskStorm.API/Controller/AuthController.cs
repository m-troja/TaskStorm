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
        l.LogInformation($"POST api/v1/auth/regenerate-tokens: {req.RefreshToken}");
        var tokenDto = await _authService.RegenerateTokensByRefreshToken(req.RefreshToken);
        l.LogDebug($"Tokens regenerated successfully by refreshToken={req.RefreshToken}, new token= {tokenDto.RefreshToken}");  

        return Ok(tokenDto);
    }

    public AuthController(ILogger<AuthController> l, IAuthService authService)
    {
        this.l = l;
        _authService = authService;
    }
}
