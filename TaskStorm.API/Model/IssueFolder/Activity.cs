using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Entity;

public abstract class Activity
{
    public int Id { get; set; }
    public ActivityType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Issue Issue { get; set; } = null!;
    public int IssueId { get; set; }
    public int CommentId { get; set; }
    public int EventAuthorUserId { get; set; }
    public string? MasterDataCode { get; set; }


    protected Activity(ActivityType type, int IssueId
        )
    {
        this.EventAuthorUserId = EventAuthorUserId;
        this.IssueId = IssueId;
        this.Type = type;
    }

    protected Activity() { }
}
