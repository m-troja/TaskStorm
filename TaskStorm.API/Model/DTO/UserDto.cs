using TaskStorm.Model.Entity;

namespace TaskStorm.Model.DTO
{
    public record UserDto(
        int Id, 
        string FirstName, 
        string LastName, 
        string Email, 
        ICollection<string> Roles, 
        List<string> Teams, 
        bool Disabled, 
        string userSlackId)
    {
    }
}
