using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;

namespace TaskStorm.Controller;

[Authorize(Roles = Role.ROLE_ADMIN)]
[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly UserCnv _userCnv;

    [HttpPut("password")]
    public async Task<ActionResult<UserDto>> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        _logger.LogInformation($"Triggered PUT api/v1/user/password for userId {req.userId}");


        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        string roles = User.FindFirstValue(ClaimTypes.Role) ?? "roles is null";

        _logger.LogInformation($"Triggered PUT api/v1/user/password for userId {req.userId}");
        _logger.LogInformation($"JWT read:  userId: {userId} roles: {roles}");

        var user = await _userService.ResetPassword(req);

        return Ok(_userCnv.ConvertUserToDto(user));
    }

    public AdminController(ILogger<AuthController> _logger, IAuthService authService, IUserService userService , UserCnv userCnv)
    {
        this._logger = _logger;
        _authService = authService;
        _userService = userService;
        _userCnv = userCnv;
    }
}
