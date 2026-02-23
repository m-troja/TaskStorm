using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskStorm.Exception.LoginException;
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
[Route("api/v1/login")]
public class LoginController : ControllerBase
{
    private readonly ILogger<LoginController> l;
    private readonly ILoginService _loginService;

    [HttpPost]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "LOGIN_OK", typeof(LoginResponse))]
    public async Task<ActionResult<Response>> Login([FromBody] LoginRequest lr)
    {
        if (lr == null)
        {
            l.LogError("Login request is null");
            return BadRequest("Request cannot be null");
        }

        try
        {
            l.LogDebug($"Received login request {lr.email}");
            var tokenDto = await _loginService.LoginAsync(lr);
            return Ok(tokenDto);
        }
        catch (InvalidEmailOrPasswordException ex)
        {
            l.LogWarning(ex, $"Invalid credentials for {lr.email}");
            return Unauthorized(new Response(ResponseType.ERROR, ex.Message));
        }
        catch (UserDisabledException ex)
        {
            l.LogWarning(ex, $"User {lr.email} is disabled");
            return Unauthorized(new Response(ResponseType.ERROR, ex.Message));
        }
        catch (System.Exception ex)
        {
            l.LogError(ex, $"Unexpected error for {lr.email}");
            return StatusCode(500, new Response(ResponseType.ERROR, "Unexpected error during login"));
        }
    }

    public LoginController(ILogger<LoginController> l, ILoginService loginService)
    {
        this.l = l;
        _loginService = loginService;
    }
}
