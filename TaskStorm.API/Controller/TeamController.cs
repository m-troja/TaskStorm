using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStorm.Log;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Request;
using TaskStorm.Service;

namespace TaskStorm.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/v1/team")]
    public class TeamController : ControllerBase
    {
        private readonly IUserService _us;
        private readonly TeamCnv _teamCnv;
        private readonly ILogger<TeamController> _logger;
        private readonly ITeamService _ts;

        public TeamController(IUserService us, TeamCnv teamCnv, ILogger<TeamController> logger, ITeamService ts)
        {
            _us = us;
            _teamCnv = teamCnv;
            _logger = logger;
            _ts = ts;
        }

        [HttpGet("id/{id:int}")]
        public async Task<ActionResult<TeamDto>> GetTeamById(int id)
        {
            _logger.LogDebug($"Fetching team by id: {id}");
            var team = await _ts.GetTeamByIdAsync(id);
            if (team == null)
                return NotFound($"Team with id={id} not found");

            return Ok(_teamCnv.ConvertTeamToTeamDto(team));
        }
        [HttpDelete("id/{id:int}")]
        public async Task<ActionResult<string>>DeleteTeamById(int id)
        {
            _logger.LogDebug($"Delete team by id: {id}");
            await _ts.DeleteTeamById(id);

            return Ok($"Deleted team id={id}");
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<TeamDto>>> GetAllTeams()
        {
            _logger.LogDebug("Fetching all teams");
            var teams = await _ts.GetAllTeamsAsync();
            return Ok(_teamCnv.ConvertTeamsToTeamDtos(teams));
        }

        [HttpPost("create")]
        public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] CreateTeamRequest req)
        {
            _logger.LogDebug($"Creating team with name: {req.Name}");
            var team = await _ts.AddTeamAsync(req);
            return Ok(_teamCnv.ConvertTeamToTeamDto(team));
        }

        [HttpGet("issues/{teamId:int}")]
        public async Task<ActionResult<List<IssueDto>>> GetIssuesByTeamId(int teamId)
        {
            _logger.LogDebug($"Fetching issues by teamId {teamId}");
            var issues = await _ts.GetIssuesByTeamId(teamId);
            return Ok(issues);
        }

        [HttpGet("users/{teamId:int}")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByTeamId(int teamId)
        {
            _logger.LogDebug($"Fetching users by teamId {teamId}");
            var users = await _ts.GetUsersByTeamId(teamId); 
            return Ok(users);
        }

        [HttpPut("{teamId:int}/add-user/{userId:int}")]
        public async Task<ActionResult<TeamDto>> AddUserToTeam(int teamId, int userId)
        {
            _logger.LogDebug($"Triggered adding user {userId} to team {teamId}");
            var team = await _ts.AddUserIntoTeam(teamId, userId);
            return Ok(_teamCnv.ConvertTeamToTeamDto(team));
        }

        [HttpPut("{teamId:int}/remove-user/{userId:int}")]
        public async Task<ActionResult<TeamDto>> RemoveUserFromTeam(int teamId, int userId)
        {
            _logger.LogDebug($"Triggered removing user {userId} to team {teamId}");
            var team = await _ts.RemoveUserFromTeam(teamId, userId);
            return Ok(_teamCnv.ConvertTeamToTeamDto(team));
        }
    }
}
