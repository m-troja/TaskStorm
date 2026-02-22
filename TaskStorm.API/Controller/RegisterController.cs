using Microsoft.AspNetCore.Mvc;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Service;
using TaskStorm.Exception.Registration;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Exception.Tokens;

namespace TaskStorm.Controller;

[ApiController]
[Route("api/v1/register")]
public class RegisterController : ControllerBase
{
    private readonly IRegisterService _rs;
    private readonly ILogger<RegisterController> _logger;
    private readonly UserCnv _userCnv;

    public RegisterController(IRegisterService rs, ILogger<RegisterController> logger, UserCnv userCnv)
    {
        _rs = rs;
        _logger = logger;
        _userCnv = userCnv;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> RegisterUser([FromBody] RegistrationRequest req)
    {
        _logger.LogInformation("Received RegisterUser req: FirstName={} LastName={} email={} SlackUserId={}", req.FirstName, req.LastName, req.Email, req.SlackUserId);
        
        if (req == null)
        {
            _logger.LogError("req == null -> throwing BadRequest");
            return BadRequest("Request cannot be null");
        }

        try
        {
            var user = await _rs.Register(req);
            var dto = _userCnv.ConvertUserToDto(user);
            return Ok(dto);
        }
        catch (RegisterEmailException ex)
        {
            _logger.LogWarning(ex, "Registration failed due to invalid email address");
            return BadRequest(new { Message = ex.Message });
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Registration failed: user already exists");
            return Conflict(new { Message = ex.Message });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            return StatusCode(500, new { Message = "Unexpected error during registration" });
        }
    }
}
