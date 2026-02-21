namespace TaskStorm.Model.DTO;

public class ProjectDto
{
    public int Id { get; set; }
    public string ShortName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<IssueDto> Issues { get; set; } = new List<IssueDto>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
