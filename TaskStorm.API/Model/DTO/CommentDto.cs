using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO;

public record CommentDto(
    int Id, 
    int IssueId, 
    string Content, 
    int AuthorId, 
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string authorName,
    ICollection<int>? attachmentIds,
    string authorSlackId
    )
{
}
