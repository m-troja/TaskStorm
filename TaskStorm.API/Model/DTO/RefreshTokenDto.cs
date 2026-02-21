namespace TaskStorm.Model.DTO;

public record RefreshTokenDto (
    string Token,
    DateTime Expires
)
{}
