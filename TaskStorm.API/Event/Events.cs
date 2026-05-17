using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Event;

public record IssueCreatedEvent(Issue Issue);
public record IssueAssignedEvent(int IssueId, int OldAssigneeId, int NewAssigneeId, int EventAuthorId, string IssueKey, string OldAssigneeName, string NewAssigneeName, string IssueTitle, string EventAuthorName);