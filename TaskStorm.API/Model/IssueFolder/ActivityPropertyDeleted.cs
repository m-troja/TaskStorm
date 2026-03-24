using TaskStorm.Model.Entity;

namespace TaskStorm.Model.IssueFolder;

public class ActivityPropertyDeleted : Activity
{

    public ActivityPropertyDeleted(ActivityType Type, int issueId, int EventAuthorUserId) : base(Type, issueId)
    {
        this.EventAuthorUserId = EventAuthorUserId;
        this.IssueId = issueId;
        this.Type = Type;

    }

    public ActivityPropertyDeleted() { }
}
