namespace TaskStorm.Model.Request;

public record UpdateDueDateRequest(int IssueId, DateTime? DueDate)
{
}
