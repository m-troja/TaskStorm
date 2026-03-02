namespace TaskStorm.Model.Request;

public record RenameIssueRequest(int IssueId, string newTitle)
{
}
