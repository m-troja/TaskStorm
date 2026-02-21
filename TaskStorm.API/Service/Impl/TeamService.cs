using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using TaskStorm.Data;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service;

public class TeamService : ITeamService
{
    private readonly PostgresqlDbContext _db;
    private readonly ILogger<TeamService> l;
    private readonly IssueCnv _issueCnv;
    private readonly UserCnv _userCnv;

    public TeamService(PostgresqlDbContext db, ILogger<TeamService> l, IssueCnv issueCnv, UserCnv userCnv)
    {
        _db = db;
        this.l = l;
        _issueCnv = issueCnv;
        _userCnv = userCnv;
    }

    private void ValidateTeamName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            l.LogDebug("Team name is null or empty");
            throw new ArgumentException("Team name cannot be null or empty");
        }
    }

    public async Task<List<Team>> GetAllTeamsAsync()
    {
        l.LogDebug("Getting all teams from the db");
        return await _db.Teams
            .Include(t => t.Users)
            .ToListAsync();
    }

    public async Task<Team> GetTeamByIdAsync(int id)
    {
        l.LogDebug($"Getting team by id: {id}");
        var team = await _db.Teams
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (team == null)
        {
            l.LogDebug($"Team with id {id} not found");
            throw new KeyNotFoundException($"Team with id {id} not found");
        }
        return team;
    }

    public async Task<Team> AddTeamAsync(CreateTeamRequest req)
    {
        ValidateTeamName(req.Name);

        var existingTeam = await _db.Teams.FirstOrDefaultAsync(t => t.Name == req.Name);
        if (existingTeam != null)
        {
            l.LogDebug($"Team with name {req.Name} already exists");
            throw new ArgumentException($"Team with name {req.Name} already exists");
        }

        var newTeam = new Team(req.Name);
        await _db.Teams.AddAsync(newTeam);
        await _db.SaveChangesAsync();
        return newTeam;
    }

    public async Task<List<IssueDto>> GetIssuesByTeamId(int teamId)
    {
        var team = await GetTeamByIdAsync(teamId);
        var issues = await _db.Issues.Where(i => i.TeamId == teamId)
            .Include(i => i.Comments).ThenInclude(c => c.Author)
            .Include(i => i.Team)
            .Include(i => i.Key)
            .Include(i => i.Project)
            .Include(i => i.Comments)
            .ToListAsync();
        l.LogDebug($"Found {issues.Count} issues in team with id {teamId}");
        return _issueCnv.ConvertIssueListToIssueDtoList(issues);
    }

    public async Task<List<UserDto>> GetUsersByTeamId(int teamId)
    {
        var users = await _db.Users
              .Where(u => u.Teams.Any(t => t.Id == teamId))
              .ToListAsync();

        l.LogDebug($"Found {users.Count} users in team {teamId}");
        return _userCnv.ConvertUsersToUsersDto(users);
    }

    public async Task<Team> AddUserIntoTeam(int teamId, int userId)
    {
        var user = await _db.Users
             .Include(u => u.Teams)
             .FirstOrDefaultAsync(u => u.Id == userId)
             ?? throw new UserNotFoundException($"User {userId} not found");

        var team = await _db.Teams
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == teamId)
            ?? throw new KeyNotFoundException($"Team {teamId} not found");

        if (user.Teams.Any(t => t.Id == teamId))
            throw new ArgumentException($"User {userId} already in team {teamId}");
        user.Teams.Add(team);
        await _db.SaveChangesAsync();
        l.LogDebug($"User with id: {userId} added to team with id: {teamId}");
        return team;
    }

    public async Task<Team> RemoveUserFromTeam(int teamId, int userId)
    {
        Team teamById = await GetTeamByIdAsync(teamId) ?? throw new KeyNotFoundException($"Team with id {teamId} was not found");
        User userById = teamById.Users?.FirstOrDefault(u => u.Id == userId) ?? throw new UserNotFoundException("User by id " + userId + "' was not found in team with id: " + teamId);

        if (teamById.Users == null)
        {
            l.LogDebug($"Team {teamId} contains no users");
            throw new ArgumentException($"Team {teamId} contains no users");
        }
        else if (!teamById.Users.Any(u => u.Id == userId)) {
            l.LogDebug($"User with id: {userId} is not in team with id: {teamId}");
            throw new ArgumentException($"User with id: {userId} is not in team with id: {teamId}");
        }

        teamById.Users.Remove(userById);
        _db.Teams.Update(teamById);
        await _db.SaveChangesAsync();
        l.LogDebug($"User with id: {userId} removed from team with id: {teamId}");
        return teamById;
    }
    public async Task DeleteTeamById(int teamId)
    {
        var team = await GetTeamByIdAsync(teamId);
        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();
        l.LogDebug($"Deleted team with id: {teamId}");
    }


}