namespace TaskStorm.Model.Request;

public record AssignIssueRequest(int IssueId, int AssigneeId)
{
}
