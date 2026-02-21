namespace TaskStorm.Model.Request;

public record CreateCommentRequest(string Content, int AuthorId, int IssueId)
{
}
