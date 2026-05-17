using System.ComponentModel.DataAnnotations.Schema;

namespace TaskStorm.Event;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }      
    public int EventAuthorId { get; set; } 
    public int IssueId { get; set; }  
    public string Key { get; set; } = string.Empty;
    public EventType Type { get; set; } 
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> Properties { get; set; } = new();

}
