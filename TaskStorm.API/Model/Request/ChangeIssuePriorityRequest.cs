namespace TaskStorm.Model.Request;

public record ChangeIssuePriorityRequest(int IssueId, string NewPriority)
{
}
