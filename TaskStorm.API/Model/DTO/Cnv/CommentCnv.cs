using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO.Cnv;

public class CommentCnv
{
    private readonly ILogger<CommentCnv> l;
    public CommentDto EntityToDto(Comment comment)
    {
 
        var authorFullName = comment.Author != null
            ? $"{comment.Author.FirstName} {comment.Author.LastName}"
            : string.Empty;
        var authorSlackUserId = comment.Author != null
            ? $"{comment.Author.SlackUserId}"
            : string.Empty;

        l.LogDebug("Converting Comment entity to CommentDto. Id: {Id}, ContentLength: {Length}, Author: {Author}", comment.Id, comment.Content?.Length ?? 0, authorFullName);
        
        var attachmentIds = (comment.Attachments ?? Enumerable.Empty<CommentAttachment>())
             .Select(a => a.Id)
             .ToList();
        var dto= new CommentDto(
            comment.Id,
            comment.IssueId,
            comment.Content ?? "",
            comment.AuthorId,
            comment.CreatedAt,
            comment.UpdatedAt,
            authorFullName,
            attachmentIds,
            authorSlackUserId
            );
        l.LogDebug($"converted {comment} to {dto}");
        return dto;
    }

    public ICollection<CommentDto> EntityListToDtoList(ICollection<Comment> comments)
    {
        l.LogDebug($"Converting list of Comment entities to list of CommentDtos. Number of comments: {comments.Count}");
        return comments.Select(EntityToDto).ToList();
    }

    public CommentCnv(ILogger<CommentCnv> _logger)
    {
        l = _logger;
    }
}
