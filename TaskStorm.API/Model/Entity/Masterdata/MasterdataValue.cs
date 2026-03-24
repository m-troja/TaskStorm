using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Entity.Masterdata;

public class MasterdataValue
{
    public int Id { get; set; }
    public ICollection<Issue> Issues { get; set; } = null!;
    public int Order { get; set; } = 0;
    public string Value { get; set; } = null!;
    public string Code { get; set; } = null!;
    public MasterdataType Type { get; set; }
    public bool IsActive { get; set; }

}
