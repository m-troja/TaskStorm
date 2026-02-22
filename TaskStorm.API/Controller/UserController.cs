using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStorm.Log;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Service;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/user")]
public class UserController : ControllerBase
{
    private readonly IUserService _us;
    private readonly UserCnv _userCnv;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService us, UserCnv userCnv, ILogger<UserController> logger)
    {
        _us = us;
        _userCnv = userCnv;
        _logger = logger;
    }

    [HttpGet("id/{id:int}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        _logger.LogDebug($"Fetching user by id: {id}");
        var user = await _us.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning($"User with id {id} not found");
            return NotFound($"User with id {id} was not found");
        }

        return Ok(_userCnv.ConvertUserToDto(user));
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        _logger.LogDebug($"Fetching user by email: {email}");
        var user = await _us.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning($"User with email {email} not found");
            return NotFound($"User with email {email} was not found");
        }

        return Ok(_userCnv.ConvertUserToDto(user));
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        _logger.LogDebug("Fetching all users");
        var users = await _us.GetAllUsersAsync();
        return Ok(_userCnv.ConvertUsersToUsersDto(users));
    }

    [HttpDelete("all")]
    public async Task<ActionResult<string>> DeleteAllUsers()
    {
        _logger.LogInformation("Triggered endpoint DeleteAllUsers");
        await _us.DeleteAllUsers();
        return Ok("All users deleted successfully");
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<string>> DeleteUserById(int id)
    {
        _logger.LogInformation($"Triggered endpoint DeleteUserById {id}");
        var user = await _us.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning($"User with id {id} not found for deletion");
            return NotFound($"User with id {id} was not found");
        }

        await _us.DeleteUserById(id);
        return Ok($"Deleted user {id}");
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        _logger.LogInformation($"Triggered endpoint UpdateUser {id}");

        var user = await _us.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning($"User with id {id} not found for update");
            return NotFound($"User with id {id} was not found");
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.SlackUserId = dto.SlackUserId;
        user.Disabled = dto.Disabled;

        await _us.UpdateUserAsync(user);

        return Ok(_userCnv.ConvertUserToDto(user));
    }
}
