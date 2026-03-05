using TaskStorm.Service;

namespace TaskStorm.Model.Event;
public class IssueCreatedEvent : IDomainEvent
{
    public string EventType => "issue.created";

    public int IssueId { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; }

    public int ProjectId { get; set; }

    public DateTime CreatedAt { get; set; }
}
