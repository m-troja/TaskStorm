using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;
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
    public async Task<Masterdata> GetMasterdataType()
    {
        _logger.LogInformation("Called controller GetMasterdataType");
        var md = await _ms.GetMasterdata();
        return md;
    }

    [HttpPost]
    public async Task<MasterdataValue> SetMasterdataValue([FromBody] CreateMasterdataValueRequest req)
    {
        _logger.LogDebug($"SetMasterdataValue req: {req}");
        var md = await _ms.CreateMasterdataValue(req);
        return md;
    }


    public MasterdataController(IMasterdataService _ms, ILogger<ProjectController> logger)
    {
        _logger = logger;
        this._ms = _ms;
    }
}
