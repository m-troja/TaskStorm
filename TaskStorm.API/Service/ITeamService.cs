using TaskStorm.Model.DTO;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;

namespace TaskStorm.Service;

public interface ITeamService
{
    Task<Team> GetTeamByIdAsync(int id);
    Task<List<Team>> GetAllTeamsAsync();
    Task<Team> AddTeamAsync(CreateTeamRequest req);

    Task<List<IssueDto>> GetIssuesByTeamId(int teamId);
    Task<List<UserDto>> GetUsersByTeamId(int teamId);
    Task<Team> AddUserIntoTeam(int teamId, int userId);
    Task<Team> RemoveUserFromTeam(int teamId, int userId);
    Task DeleteTeamById(int teamId);
}
