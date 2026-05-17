using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Entity;

public class IssueChanges
{
    public string? OldTitle { get; set; }
    public string? NewTitle { get; set; }

    public string? OldDescription { get; set; }
    public string? NewDescription { get; set; }

    public IssueStatus? OldStatus { get; set; }
    public IssueStatus? NewStatus { get; set; }

    public IssuePriority? OldPriority { get; set; }
    public IssuePriority? NewPriority { get; set; }

    public User? OldAssignee { get; set; }
    public User? NewAssignee { get; set; }

    public int? OldTeamId { get; set; }
    public int? NewTeamId { get; set; }
    public DateTime? OldDueDate { get; set; }
    public DateTime? NewDueDate { get; set; }
    public ICollection<MasterdataValue>? OldLabels { get; set; }
    public ICollection<MasterdataValue>? NewLabels { get; set; }
}
