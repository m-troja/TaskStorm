namespace TaskStorm.Exception;

public class BadRequestException(string Message) : System.Exception(Message)
{
}
