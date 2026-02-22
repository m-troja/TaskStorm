using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TaskStorm.Data;
using TaskStorm.Exception;
using TaskStorm.Exception.IssueException;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service.Impl;

public class CommentService : ICommentService
{
    private readonly PostgresqlDbContext _db;
    private readonly IIssueService _issueService;
    private readonly IUserService _userService;
    private readonly CommentCnv _commentCnv;
    private readonly ILogger<CommentService> logger;
    private readonly ISlackNotificationService _slackNotificationService;
    public async Task<CommentDto> CreateCommentAsync(CreateCommentRequest req)
    {
        var issue = await _issueService.GetIssueByIdAsync(req.IssueId);
        var user = await _userService.GetByIdAsync(req.AuthorId);
        var comment = new Comment(req.Content, user, issue)
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AuthorId = req.AuthorId,
            IssueId = req.IssueId
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        await _slackNotificationService.SendCommentAddedNotificationAsync(issue);
        return _commentCnv.EntityToDto(comment);
    }
    public async Task<CommentDto> EditCommentAsync(EditCommentRequest req)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == req.id);
;
        if (comment == null)
        {
            throw new ContentNotFoundException($"Comment with Id={req.id} not found") ;
        }
        comment.Content = req.content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return _commentCnv.EntityToDto(comment);
    }
    public async Task<IEnumerable<CommentDto>> GetCommentsByIssueIdAsync(int issueId)
    {
        List<Comment> comments = await _db.Comments.Where(c => c.IssueId == issueId)
            .Include(c => c.Author)
            .Include(c => c.Attachments)
            .ToListAsync();
        logger.LogDebug($"Retrieved {comments.Count} comments for IssueId={issueId}");
        comments.ForEach(c => logger.LogDebug($"Comment Id={c.Id}, AuthorId={c.AuthorId}, Content='{c.Content}', attachments = {c.Attachments.Count}"));
        return _commentCnv.EntityListToDtoList(comments);
    }

    public CommentService(PostgresqlDbContext db, IIssueService issueService, IUserService userService, CommentCnv commentCnv, ILogger<CommentService> logger, ISlackNotificationService slackNotificationService)
    {
        _db = db;
        _issueService = issueService;
        _userService = userService;
        _commentCnv = commentCnv;
        this.logger = logger;
        _slackNotificationService = slackNotificationService;
    }

    public async Task DeleteAllCommentsByIssueId(int issueId)
    {
        await _db.Database.ExecuteSqlAsync($"DELETE FROM Comments where issue_id={issueId}");
        logger.LogInformation($"Deleted comment where Id={issueId}");
    }
    public async Task DeleteCommentById(int id)
    {
        await _db.Database.ExecuteSqlAsync($"DELETE FROM Comments where id={id}");
        logger.LogInformation($"Deleted comment where Id={id}");
    }

}
