using TaskStorm.Model.DTO;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service;

public interface ICommentService
{
    Task<CommentDto> CreateCommentAsync(CreateCommentRequest ccr);
    Task<CommentDto> EditCommentAsync(EditCommentRequest ccr);
    Task<IEnumerable<CommentDto>> GetCommentsByIssueIdAsync(int issueId);
    Task DeleteAllCommentsByIssueId(int issueId);
    Task DeleteCommentById(int id);

}
