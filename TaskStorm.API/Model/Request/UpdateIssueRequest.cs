namespace TaskStorm.Model.Request;

public class UpdateIssueRequest
{
    public int IssueId { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? AssigneeId { get; set; }
    public string? Title { get; set; }
    public string? DueDate { get; set; }
    public int? TeamId { get; set; }
    public int? ProjectId { get; set; }
    public ICollection<MasterdataValueRequest>? MasterDataValues { get; set; }
}
