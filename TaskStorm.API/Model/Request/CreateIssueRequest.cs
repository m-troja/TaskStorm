namespace TaskStorm.Model.Request;

public record CreateIssueRequest(
    string title, 
    string? description,
    string? priority, 
    int authorId, 
    int? assigneeId, 
    string? dueDate, 
    int projectId )  {}
