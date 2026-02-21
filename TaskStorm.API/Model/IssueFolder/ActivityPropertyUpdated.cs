using TaskStorm.Model.Entity;

namespace TaskStorm.Model.IssueFolder;

public class ActivityPropertyUpdated : Activity
{
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;

    public ActivityPropertyUpdated(string FromStringProperty, string ToStringProperty, int IssueId, ActivityType Type) : base(Type, IssueId)
    {
        this.OldValue = FromStringProperty;
        this.NewValue = ToStringProperty;
        this.Type = Type;
        this.IssueId = IssueId;
    }

    public ActivityPropertyUpdated() { }
}
