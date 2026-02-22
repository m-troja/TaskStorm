using TaskStorm.Model.Entity;

namespace TaskStorm.Model.IssueFolder;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }
    public int AuthorId { get; set; }      // FK
    public int IssueId { get; set; }       // FK
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // UTC
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;   // UTC
    public User Author { get; set; }
    public Issue Issue { get; set; }
    public Comment(string content, User author, Issue issue)
    {
        Content = content;
        Author = author;
        Issue = issue;
    }

    public ICollection<CommentAttachment>? Attachments { get; set; } = new List<CommentAttachment>();
    public Comment() { }
}
