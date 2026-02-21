using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO;

public record IssueDto(
    int Id, 
    string Key, 
    string Title, 
    string Description, 
    IssueStatus Status, 
    IssuePriority Priority, 
    int AuthorId,
    int AssigneeId,
    DateTime CreatedAt, 
    DateTime? DueDate,
    DateTime? UpdatedAt, 
    ICollection<CommentDto> Comments, 
    int ProjectId,
    TeamDto Team)
{ }

