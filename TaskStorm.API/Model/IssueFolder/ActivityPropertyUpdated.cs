using TaskStorm.Model.Entity;

namespace TaskStorm.Model.IssueFolder;

public class ActivityPropertyUpdated : Activity
{
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public int userId { get; set; } = -1;


    public ActivityPropertyUpdated(string FromStringProperty, string ToStringProperty, int IssueId, ActivityType Type, int userId) : base(Type, IssueId)

    {
        this.userId = userId;
        this.OldValue = FromStringProperty;
        this.NewValue = ToStringProperty;
        this.Type = Type;
        this.IssueId = IssueId;
    }

    public ActivityPropertyUpdated() { }
}
