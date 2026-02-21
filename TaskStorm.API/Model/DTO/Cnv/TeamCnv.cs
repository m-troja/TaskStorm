using TaskStorm.Model.Entity;
using TaskStorm.Log;
namespace TaskStorm.Model.DTO.Cnv;

public class TeamCnv
{
    private readonly ILogger<TeamCnv> l;
    public TeamDto ConvertTeamToTeamDto(Team team)
    {
        l.LogDebug($"Converting Team entity to TeamDto: {team}");
        var NewTeamDto =  new TeamDto
       (
           team.Id,
           team.Name,
           team.Issues?.Select(i => i.Id).ToList() ?? new List<int>(),
           team.Users?.Select( u => u.Id).ToList() ?? new List<int>()
        );
        l.LogDebug($"Converted TeamDto: {NewTeamDto}");
        return NewTeamDto;
    }

    public IEnumerable<TeamDto> ConvertTeamsToTeamDtos(ICollection<Team> teams)
    {
        var teamDtos = new List<TeamDto>();
        foreach (var team in teams)
        {
            teamDtos.Add(ConvertTeamToTeamDto(team));
        }
        return teamDtos;
    }

    public TeamCnv(ILogger<TeamCnv> logger)
    {
        l = logger;
    }
}
