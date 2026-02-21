using System.Data;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? Password { get; set; } = null!;
        public byte[]? Salt { get; set; } = null!;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public string? SlackUserId { get; set; } = null!;
        public Boolean Disabled { get; set; } = false;
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Issue> AssignedIssues { get; set; } = new List<Issue>();
        public ICollection<Issue> AuthoredIssues { get; set; } = new List<Issue>();

        public User(string firstName, string lastName, string email, string password, byte[] salt, Role Role)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Password = password;
            Salt = salt;
            Roles = new List<Role> { Role };
        }

        public User(string slackName, string slackUserId) {
            FirstName = slackName;
            SlackUserId = slackUserId;
            Disabled = false;
        }
        public User() {}

        public override string? ToString()
        {
            return "User(Id=" + Id + ", FirstName=" + FirstName + ", LastName=" + LastName + ", Email=" + Email + ", SlackUserID=" + SlackUserId + ")";
        }

        public override bool Equals(object? obj)
        {
            return obj is User user &&
                   FirstName == user.FirstName &&
                   LastName == user.LastName &&
                   Email == user.Email &&
                   SlackUserId == user.SlackUserId &&
                   Disabled == user.Disabled;
        }
    }
}
