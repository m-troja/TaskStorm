namespace TaskStorm.Model.Request;

public record ChangeIssueStatusRequest(int IssueId, string NewStatus)
{
}
