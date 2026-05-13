namespace TaskStorm.Model.IssueFolder;

public class IssueSearchCriteria
{
    // fields

    public int? ProjectId { get; set; }
    public int? AuthorId { get; set; }
    public int? AssigneeId { get; set; }
    public IssueStatus? Status { get; set; }
    public IssuePriority? Priority { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? Description { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;


    // sort

    public string? SortBy { get; set; } = "CreatedAt"; 
    public bool IsDescending { get; set; } = true;
}