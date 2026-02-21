namespace TaskStorm.Model.Entity;

public record AccessToken
(
    string Token,
    DateTime Expires
){}