using TaskStorm.Model.Entity;
using TaskStorm.Service;

namespace TaskStorm.Model.IssueFolder
{
    public class Issue :  IAutomaticDates
    {
        public int Id { get; set; }
        public int IdInsideProject { get; set; } = 0!;
        public int ProjectId { get; set; } = 0!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public User Author { get;  set; } = null!;
        public int AuthorId { get; set; }      // FK
        public int? AssigneeId { get; set; }   // FK
        public DateTime? UpdatedAt { get; set; }  = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }    // UTC
        public IssueStatus Status { get; set; } = IssueStatus.NEW;
        public IssuePriority? Priority { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public User? Assignee { get; set; }
        public Project? Project { get; set; }
        public Key Key { get; set; } = null!;
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public Team? Team { get; set; } = null!;
        public int? TeamId { get; set; } = null!;
        public DateTime CreatedAt { get ; set ; }

        public Issue(string title, string? description, IssuePriority? priority,  User author, User? assignee, DateTime? dueDate, 
            int authorId, int? assigneeId, int projectId, int idInsideProject)
        {
            Title = title;
            Description = description;
            Author = author;
            Assignee = assignee;
            DueDate = dueDate;
            Priority = priority;
            AuthorId = authorId;
            AssigneeId = assigneeId;
            ProjectId = projectId;
            IdInsideProject = idInsideProject;
        }
        public Issue()  { }

        override
        public string ToString()
        {
            return "Issue(Id=" + Id + ", Title=" + Title + ", Description=" + Description + ", Status=" + Status + ", Priority=" + Priority + ", AuthorId=" + AuthorId + ", AssigneeId=" + AssigneeId + ", ProjectId=" + ProjectId + ", CreatedAt=" + CreatedAt + ", DueDate=" + DueDate + ", UpdatedAt=" + UpdatedAt + ")";
        }
    }
}
