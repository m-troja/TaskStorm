namespace TaskStorm.Model.Entity;

public class UpdateUserDto
{
    public string? FirstName { get; set; } 
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? SlackUserId { get; set; }
    public bool? Disabled { get; set; }
}