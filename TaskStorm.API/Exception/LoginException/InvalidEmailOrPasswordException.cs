namespace TaskStorm.Exception.LoginException;

public class InvalidEmailOrPasswordException(string message) : System.Exception(message)
{
}
