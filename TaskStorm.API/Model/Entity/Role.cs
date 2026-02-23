namespace TaskStorm.Model.Entity
{
    public class Role
    {
        public const string ROLE_USER = "ROLE_USER";
        public const string ROLE_ADMIN = "ROLE_ADMIN";

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<User> Users { get; set; } = new List<User>();

        public Role() { }

        public Role(string name)
        {
            Name = name;
        }
    }
}
