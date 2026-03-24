using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;
using TaskStorm.Exception;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.Request;
using TaskStorm.Service;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/masterdata")]
public class MasterdataController : ControllerBase
{
    private readonly IMasterdataService _ms;
    private readonly ILogger<MasterdataController> _logger;

    [HttpGet]
    public async Task<MasterdataAllTypesWithAllValuesDto> GetAllMasterdata()
    {
        _logger.LogInformation("Called controller GetAllMasterdata");
        var md = await _ms.GetAllMasterdata();
        return md;
    }

    [HttpPost]
    public async Task<ActionResult<MasterdataSingleTypeWithValuesDto>> SetMasterdataValue([FromBody] MasterdataValueRequest req)
    {
        _logger.LogInformation("Called controller SetMasterdataValue req: {req}", req);

        MasterdataSingleTypeWithValuesDto md;
        try
        {
            md = await _ms.SetMasterdataValue(req);

        }
        catch (BadRequestException e)
        {
            return BadRequest("Type or value already exists");
        }

        return Ok(md);
    }

    [HttpGet("type")]
    public async Task<ActionResult<MasterdataSingleTypeWithValuesDto>> GetMasterdataValuesForType([FromQuery] MasterdataType type)
    {
        _logger.LogDebug($"Called controller GetMasterdataValuesForType MasterdataType: {type}");
        var md = await _ms.GetMasterdataValuesForType(type);
        if (md == null)
        {
            return NotFound();
        }
        return Ok(md);
    }


    public MasterdataController(IMasterdataService _ms, ILogger<MasterdataController> _logger)
    {
        this._logger = _logger;
        this._ms = _ms;
    }
}
