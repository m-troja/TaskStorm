using TaskStorm.Model.Entity;

namespace TaskStorm.Model.DTO
{
    public record ActivityDto
        (
            ActivityType ActivityType,
            int? oldUserId,
            int? newUserId,
            int? createdIssueId,
            int? updatedIssueId,
            int? createdCommentId,
            int? updatedCommentId,
            int? authorUserId,
            string? oldValue,
            string? newValue,
            DateTime Timestamp
        )
    {
    }
}
