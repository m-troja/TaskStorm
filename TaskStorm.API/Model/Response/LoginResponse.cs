namespace TaskStorm.Model.Response;

public record LoginResponse(ResponseType responseType, string accessToken)
{
}
