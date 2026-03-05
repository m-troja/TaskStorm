namespace TaskStorm.Model.Event;

public class IssueChangedEvent
{
    public int IssueId { get; set; }
    public string FieldChanged { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int AuthorId { get; set; }
    public int? CommentId { get; set; } 
}