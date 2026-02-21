namespace TaskStorm.Model.Request;

public record SlackCreateIssueRequest(
    string title, 
    string description, 
    string? priority,
    string authorSlackId,
    string? assigneeSlackId,
    string? dueDate, 
    int? projectId 
    ){}
