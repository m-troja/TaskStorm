using TaskStorm.Service;

namespace TaskStorm.Model.Event;

public class IssueAssignedEvent : IDomainEvent
{
    public string EventType => "issue.assigned";

    public int IssueId { get; set; }

    public int OldAssigneeId { get; set; }

    public int NewAssigneeId { get; set; }

    public int AuthorId { get; set; }
}
