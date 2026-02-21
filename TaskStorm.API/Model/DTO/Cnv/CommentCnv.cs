using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO.Cnv;

public class CommentCnv
{
    private readonly ILogger<CommentCnv> l;
    public CommentDto EntityToDto(Comment comment)
    {
        l.LogDebug("Converting Comment entity to CommentDto. {id} {content} {author}", comment.Id, comment.Content, comment.Author.FirstName + " " + comment.Author.LastName);

        return new CommentDto(
            comment.Id, 
            comment.IssueId,
            comment.Content, 
            comment.AuthorId, 
            comment.CreatedAt,
            comment.UpdatedAt,
            comment.Author.FirstName + " " + comment.Author.LastName
            );
    }

    public ICollection<CommentDto> EntityListToDtoList(ICollection<Comment> comments)
    {
        l.LogDebug($"Converting list of Comment entities to list of CommentDtos. Number of comments: {comments.Count}");
        ICollection<CommentDto> commentDtos = new List<CommentDto>();
        foreach (var comment in comments)
        {
            commentDtos.Add(EntityToDto(comment));
        }
        return commentDtos;
    }

    public CommentCnv(ILogger<CommentCnv> _logger)
    {
        l = _logger;
    }
}
