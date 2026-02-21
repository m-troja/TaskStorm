using TaskStorm.Service;

namespace TaskStorm.Model.IssueFolder;

public class Project 
{
    public int Id { get; set; }
    public string ShortName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get ; set; }
    public ICollection<Issue>? Issues { get; set; } = new List<Issue>();
    public ICollection<Key>? Keys { get; set; } = new List<Key>();  
    public Project(string shortName, string description)
    {
        ShortName = shortName;
        Description = description;
    }
    public Project(int id, string shortName, string description, DateTime date)
    {
        Id = id;
        ShortName = shortName;
        Description = description;
        CreatedAt = date;
    }



    public Project()
    {
    }
}
