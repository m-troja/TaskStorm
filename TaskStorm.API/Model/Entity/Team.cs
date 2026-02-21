using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Entity;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Issue>? Issues { get; set; } = new List<Issue>();
    public ICollection<User>? Users { get; set; } = new List<User>();

    public Team(string name)
    {
        Name = name;
    }
}
