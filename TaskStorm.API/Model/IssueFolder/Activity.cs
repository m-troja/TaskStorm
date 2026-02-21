using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Entity;

public abstract class Activity
{
    public int Id { get; set; }
    public ActivityType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Issue Issue { get; set; } = null!;
    public int IssueId { get; set; }


    protected Activity(ActivityType type, int IssueId)
    {
        this.IssueId = IssueId;
        this.Type = type;
    }

    protected Activity() { }
}
