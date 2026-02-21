namespace TaskStorm.Model.Request
{
    public record RegistrationRequest(string FirstName, 
        string LastName, 
        string Email, 
        string Password, 
        string SlackUserId)
    {
    }
}
