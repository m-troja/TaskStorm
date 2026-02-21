namespace TaskStorm.Model.Request;

public record RenameIssueRequest(int id, string newTitle)
{
}
