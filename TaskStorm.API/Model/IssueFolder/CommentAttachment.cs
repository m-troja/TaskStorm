namespace TaskStorm.Model.IssueFolder;

public class CommentAttachment
{
    public int Id { get; set; }
    public Comment Comment { get; set; }
    public int CommentId { get; set; }
    public string Guid { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public DateTime Uploaded { get; set; } = DateTime.UtcNow;

    public CommentAttachment() { }
}
