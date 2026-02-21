using System.Runtime.InteropServices;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO.Cnv;

public class IssueCnv
{
    private readonly CommentCnv _commentCnv;
    private readonly ILogger<IssueCnv> logger;
    private readonly TeamCnv teamCnv;
    

    public IssueDto ConvertIssueToIssueDto(Issue Issue)
    {
        ICollection<CommentDto> commentDtos = _commentCnv.EntityListToDtoList(Issue.Comments);
        TeamDto teamDto = Issue.Team is not null
            ? teamCnv.ConvertTeamToTeamDto(Issue.Team)
            : new TeamDto(-1, "", new List<int>(), new List<int>());

        var issueDto = new IssueDto(
            Issue.Id,
                Issue.Key.KeyString,
                Issue.Title,
                Issue.Description ?? "No description",
                Issue.Status,
                Issue.Priority ?? IssuePriority.NORMAL,
                Issue.AuthorId,
                Issue.AssigneeId ?? 0,
                Issue.CreatedAt,
                Issue.DueDate,
                Issue.UpdatedAt,
                commentDtos,
                Issue.ProjectId,
                teamDto
            );
        
        return issueDto;
    }
    public IssueDtoChatGpt ConvertIssueToIssueDtoChatGpt(Issue Issue)
    {
        logger.LogDebug($"Converting {Issue} to IssueDtoChatGpt");
        logger.LogDebug($"Assignee is {Issue.Assignee}, Id {Issue.AssigneeId}");
        logger.LogDebug($"Author is {Issue.Author}, Id {Issue.AuthorId}");
        ICollection<CommentDto> commentDtos = _commentCnv.EntityListToDtoList(Issue.Comments);
        
        var issueDto = new IssueDtoChatGpt(
            Issue.Id,
                Issue.Key.KeyString,
                Issue.Title,
                Issue.Description ?? "No description",
                Issue.Status,
                Issue.Priority ?? IssuePriority.NORMAL,
                Issue.Author.SlackUserId ?? "Empty",
                Issue.Assignee?.SlackUserId ?? "Empty",
                Issue.CreatedAt,
                Issue.DueDate,
                Issue.UpdatedAt,
                commentDtos,
                Issue.ProjectId
            );
        logger.LogInformation($"Converted IssueId: {Issue.Id} to {issueDto}");
        return issueDto;
    }
    public List<IssueDto> ConvertIssueListToIssueDtoList(List<Issue> issues)
    {
        var issueDtos = new List<IssueDto>();
        foreach (var issue in issues)
        {
            issueDtos.Add(ConvertIssueToIssueDto(issue));
        }
        return issueDtos;
    }

    public IssueCnv(CommentCnv commentCnv, ILogger<IssueCnv> logger, TeamCnv teamCnv)
    {
        _commentCnv = commentCnv;
        this.logger = logger;
        this.teamCnv = teamCnv;
    }
}
