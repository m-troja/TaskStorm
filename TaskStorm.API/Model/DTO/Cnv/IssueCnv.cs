using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using TaskStorm.Model.DTO.ChatGpt;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO.Cnv;

public class IssueCnv
{
    private readonly CommentCnv _commentCnv;
    private readonly ILogger<IssueCnv> logger;
    private readonly TeamCnv teamCnv;
    

    public IssueDto EntityToDto(Issue Issue)
    {
        var labelsStrings = new List<string>();
        if (Issue.Labels != null) labelsStrings = Issue.Labels.Select(label => label.Value).ToList();


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
                Issue.ProjectId,
                Issue.TeamId ?? -1,
                labelsStrings

            );
        
        return issueDto;
    }
    public IssueDtoChatGpt EntityToIssueDtoChatGpt(Issue Issue)
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
                Issue.ProjectId,
                Issue.Team?.Name ?? "No Team"
            );
        logger.LogInformation($"Converted IssueId: {Issue.Id} to {issueDto}");
        return issueDto;
    }
    public List<IssueDto> EntityListToDtoList(List<Issue> issues)
    {
        var issueDtos = new List<IssueDto>();
        foreach (var issue in issues)
        {
            issueDtos.Add(EntityToDto(issue));
        }
        return issueDtos;
    }

    public List<IssueDtoChatGpt> EntityListToChatGptDtoList(IEnumerable<Issue> issues)
    {
        var issueDtos = new List<IssueDtoChatGpt>();
        foreach (var issue in issues)
        {
            issueDtos.Add(EntityToIssueDtoChatGpt(issue));
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
