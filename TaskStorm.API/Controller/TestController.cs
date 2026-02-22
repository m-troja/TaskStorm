using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Claims;
using TaskStorm.Log;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Service;

namespace TaskStorm.Controller;

[ApiController]
[Route("api/v1/test")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> l;
    private readonly IUserService _us;
    private readonly UserCnv _userCnv;

    [HttpGet("env")]
    public ActionResult<String> TestDeploy()
    {
        l.LogDebug("Test controller entered");

        //Env vars check
        string env = "";
        foreach (System.Collections.DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
            string key = envVar.Key.ToString() ?? "";
            env = env + envVar.Key + " = " + envVar.Value + "\n";
            l.LogDebug($"EnvVar: {envVar.Key} = {envVar.Value}");

        }
        return Ok(env); 
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var version = Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        return Ok(new { version });
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfileByAccessToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Token invalid or missing" });
        }

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID in token" });
        }

        var user = await _us.GetByIdAsync(userId);
        
        return Ok(_userCnv.ConvertUserToDto(user));
    }
    [Authorize]
    [HttpGet("profile-authorized")]
    public async Task<ActionResult<UserDto>> AuthorizedGetProfileByAccessToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Invalid token: missing user ID" });
        }

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID in token" });
        }

        var user = await _us.GetByIdAsync(userId);

        return Ok(_userCnv.ConvertUserToDto(user));
    }
    public TestController(ILogger<TestController> l, IUserService us, UserCnv userCnv)
    {
        Console.WriteLine(">>> ENTER TestController constructor");
        this.l = l;
        _us = us;
        _userCnv = userCnv;
        Console.WriteLine(">>> EXIT TestController constructor");
    }
}
